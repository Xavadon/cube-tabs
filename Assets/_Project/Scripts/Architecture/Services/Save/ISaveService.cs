namespace _Project.Scripts.Architecture.Services.Save
{
    public interface ISaveService : IService
    {
        void Save(SaveData data);
        SaveData Load();
        bool HasSave();
        void DeleteSave();

        /// <summary> ///
        /// Немедленный пуш в облако в обход троттлинга. Нужен перед consume покупки:
        /// по докам Yandex сначала сохраняем данные игрока, только потом потребляем покупку.
        /// </summary>
        void ForceSync();
    }
}
