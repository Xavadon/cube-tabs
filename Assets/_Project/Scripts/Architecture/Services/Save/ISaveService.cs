namespace _Project.Scripts.Architecture.Services.Save
{
    public interface ISaveService : IService
    {
        void Save(SaveData data);
        SaveData Load();
        bool HasSave();
        void DeleteSave();
    }
}
