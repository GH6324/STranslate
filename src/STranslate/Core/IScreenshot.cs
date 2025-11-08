using System.Drawing;

namespace STranslate.Core;

/// <summary>
/// 截图接口，可以不要定义接口，懒得删了
/// </summary>
public interface IScreenshot
{
    /// <summary>
    /// 获取截图
    /// </summary>
    /// <returns></returns>
    Bitmap? GetScreenshot();
    
    /// <summary>
    /// 异步获取截图
    /// </summary>
    /// <returns></returns>
    Task<Bitmap?> GetScreenshotAsync();
}
