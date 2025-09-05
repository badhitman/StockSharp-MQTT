using SharedLib;
using Telegram.Bot.Types;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Tools
/// </summary>
public static class ToolsStatic
{
    /// <summary>
    /// Convert File
    /// </summary>
    public static IAlbumInputMedia ConvertFile(FileAttachModel sender_file)
    {
        if (GlobalToolsStandard.IsImageFile(sender_file.Name))
            return new InputMediaPhoto(InputFile.FromStream(new MemoryStream(sender_file.Data), sender_file.Name));

        return new InputMediaDocument(InputFile.FromStream(new MemoryStream(sender_file.Data), sender_file.Name));
    }
}