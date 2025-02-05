namespace DayTradeBot.common.util.serializationUtil;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using DayTradeBot.domains.tradeEngine.core;

public class SerializationUtil
{
    public static JsonSerializerOptions GetJsonSerlializationOptions()
    {
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
    public static void SerializePositionsBySymbol(Dictionary<string, Position> positions, string filePath)
    {
        var options = GetJsonSerlializationOptions();
        // Serialize the positions dictionary to JSON
        string json = JsonSerializer.Serialize(positions, options);

        // Write the JSON data to the file
        File.WriteAllText(filePath, json);
    }

    public static Dictionary<string, Position>? DeserializePositionsBySymbol(string filePath)
    {
        var options = GetJsonSerlializationOptions();

        // Read the JSON data from the file
        string json = File.ReadAllText(filePath);

        // Deserialize the JSON data to a dictionary of positions
        var positions = JsonSerializer.Deserialize<Dictionary<string, Position>>(json, options);

        return positions;
    }
}
