namespace MqttGraphiteBridge
{
    public interface IEndpoint
    {
        string Host { get; set; }
        int Port { get; set; }
        string Topic { get; set; }
        string UserName { get; set; }
        string Password { get; set; }
    }
}