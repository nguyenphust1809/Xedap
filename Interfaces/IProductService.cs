namespace Xedap.Interfaces
{
    public interface IProductService
    {
        void ImportExcel(string filePath, int categoryId);
        void Save();
    }
}
