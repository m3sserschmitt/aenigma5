namespace Enigma5.Structures.Contracts;

public interface IOnionBuilder
{
    IOnion Build();

    ISetMessageNextAddress AddPeel();
}