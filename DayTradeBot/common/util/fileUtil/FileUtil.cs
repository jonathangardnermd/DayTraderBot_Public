namespace DayTradeBot.common.util.fileUtil;

using Newtonsoft.Json.Linq;

public static class FileUtil
{
    public static void EnsureDirectoryExists(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
    }

    public static JObject LoadJsonFromFile(string filePath)
    {
        string jsonContent = File.ReadAllText(filePath);
        JObject apiKeysObject = JObject.Parse(jsonContent);
        return apiKeysObject;
    }

    public static void DeleteFilesInFolder(string folderPath)
    {
        DirectoryInfo directory = new DirectoryInfo(folderPath);

        if (!directory.Exists)
        {
            return;
        }

        FileInfo[] files = directory.GetFiles();
        foreach (FileInfo file in files)
        {
            file.Delete();
        }
    }
}