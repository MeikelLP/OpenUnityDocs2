using System.Threading.Tasks;

namespace OpenUnityDocs.Converter
{
    public interface IConverter
    {
        Task<string> ConvertAsync(string filePath);
        string OutFileEnding { get; }
        string InFileEnding { get; }
    }
}