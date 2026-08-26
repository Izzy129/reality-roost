using System.Threading.Tasks;

public interface IRuntimeImporter
{
    public Task ImportFromWeb(string uri);
    public void StartContent();
    public void ClearContent();
}
