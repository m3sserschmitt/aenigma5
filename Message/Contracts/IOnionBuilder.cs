namespace Message.Contracts;

public interface IOnionBuilder
{
    IOnion Build();

    ISetMessageNextAddress AddPeel();
}