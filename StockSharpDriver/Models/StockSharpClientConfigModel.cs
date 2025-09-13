////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace StockSharpDriver;

public partial class StockSharpClientConfigMainModel : StockSharpClientConfigModel
{
    public Ecng.Logging.LogLevels LogLevelEcng { get; set; }


    /// <inheritdoc/>
    public new static StockSharpClientConfigMainModel BuildEmpty()
    {
        return new StockSharpClientConfigMainModel() { Scheme = "mqtt", Port = 1883 };
    }
}