using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Text;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Identity.Client;
using Google;
using System.Net.Mail;
using System.Text.Json;

namespace App_Clinica.Consumidor_de_Eventos
{
    public class ReceivedMessage
    {
        public string CorreoDestinatario { get; set; }
        public string MensajeActualizado { get; set; }
    }
    public class EventConsumerService : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private readonly ILogger<EventConsumerService> _logger;
        private readonly string[] _queues = { "queue_citamedica", "queue_registro", "queue_notificacioncita"};
        private readonly string[] _routingKeys = { "CitaCreada", "RegistroCreado", "NotificacionCita"};
        public EventConsumerService(ILogger<EventConsumerService> logger)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger = logger;

            var exchange = "clinic_events";

            // Declarar el exchange persistente
            _channel.ExchangeDeclare(exchange: exchange, type: "topic", durable: true);

            // Declarar y enlazar cada cola con su clave de enrutamiento
            for (int i = 0; i < _queues.Length; i++)
            {
                string queue = _queues[i];
                string routingKey = _routingKeys[i];

                // Declarar una cola persistente
                _channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // Enlazar la cola al exchange con su clave de enrutamiento
                _channel.QueueBind(queue: queue, exchange: exchange, routingKey: routingKey);

                _logger.LogInformation($"Queue '{queue}' bound to exchange '{exchange}' with routing key '{routingKey}'.");

                // Crear el consumidor
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var messageJson = Encoding.UTF8.GetString(body); // Decodifica el mensaje si es texto.

                        // Confirmar recepción exitosa del mensaje
                        _channel.BasicAck(ea.DeliveryTag, false);

                        var deserializedMessage = JsonSerializer.Deserialize<ReceivedMessage>(messageJson);

                        if (deserializedMessage != null)
                        {
                            var correoDestinatario = deserializedMessage.CorreoDestinatario;
                            var mensajeActualizado = deserializedMessage.MensajeActualizado;

                            // Enviar el correo usando los datos deserializados
                            SendEmailAsync(correoDestinatario, mensajeActualizado, routingKey).Wait();

                            _logger.LogInformation($"Mensaje procesado: Correo: {correoDestinatario}, Mensaje: {mensajeActualizado}");
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando el mensaje");

                        _channel.BasicNack(ea.DeliveryTag, false, true);
                    }

                };

                // Iniciar la escucha de la cola con autoAck: false
                _channel.BasicConsume(queue, false, consumer);
            }

        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Establecer el comportamiento de cancelación
            stoppingToken.Register(() =>
            {
                // Cerrar la conexión y el canal al finalizar el servicio
                _channel.Close();
                _connection.Close();
            });

            // Este método mantendrá el servicio en ejecución
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            // Asegúrate de liberar recursos cuando el servicio se detenga
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }

        private static string[] Scopes = { GmailService.Scope.GmailSend };
        private static string ApplicationName = "KEVIN";

        public async Task SendEmailAsync(string correodestinatario, string mensaje, string Subject)
        {
            UserCredential credential;

            try
            {
                // El archivo token.json guarda el acceso del usuario, no lo compartas
                var credPath = "token.json";
                using (var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    // Intenta autorizar al usuario
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                // Crear el servicio de Gmail con OAuth 2.0
                var service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                // Crear el correo con MimeKit
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("EPS SURA", "kevinsepulveda09@gmail.com"));
                emailMessage.To.Add(new MailboxAddress("Destinatario", correodestinatario));
                emailMessage.Subject = Subject;
                emailMessage.Body = new TextPart(TextFormat.Plain) { Text = mensaje };

                var msg = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = Base64UrlEncode(emailMessage.ToString())
                };

                // Enviar el correo
                await service.Users.Messages.Send(msg, "me").ExecuteAsync();
                _logger.LogInformation($"Email se envió correctamente");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogInformation($"Archivo no encontrado: {ex.Message}");
            }
            catch (GoogleApiException ex)
            {
                _logger.LogInformation($"Error de API de Google: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error inesperado: {ex.Message}");
            }
        }

        private static string Base64UrlEncode(string input)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').Replace("=", "");
        }
    }
}
