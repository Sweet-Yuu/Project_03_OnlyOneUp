public interface IPlayerNameStore
{
    string Load();
    void Save(string name);
}
