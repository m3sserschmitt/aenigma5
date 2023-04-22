using Message.Contracts;

namespace Message;

public class MessageBuilder
{
    private class Impl :
        ISetMessageContent,
        IEncryptMessage,
        IMessageBuilder
    {
        private Message Message = new();

        public IMessage Build()
        {
            return Message;
        }

        public IMessageBuilder Encrypt()
        {
            return this;
        }

        public IEncryptMessage SetMessageContent(byte[] content)
        {
            Message.Content = (byte[]) content.Clone();
            return this;
        }
    }

    public static ISetMessageContent Create()
    {
        return new Impl();
    }
}
