namespace Enigma5.Message.Contracts;

public interface IOnionBuilder
{
    IOnion Build();

    ISetMessageNextAddress AddPeel();
}