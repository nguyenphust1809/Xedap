using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Xedap.Models
{
    public class PredictionResult
    {
        public string ClassName { get; set; }  
        public float Confidence { get; set; }
        public List<ProductModel> RelatedProducts { get; set; }

    }
}
