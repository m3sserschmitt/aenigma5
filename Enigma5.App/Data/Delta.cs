namespace Enigma5.App.Data;

public class Delta
{
    public Delta()
    {
        Vertex = null;
        Added = false;
    }

    public Delta(Vertex? vertex, bool added)
    {
        Vertex = vertex;
        Added = added;    
    }

    public Vertex? Vertex { get; set; }

    public bool Added { get; set; }
}
