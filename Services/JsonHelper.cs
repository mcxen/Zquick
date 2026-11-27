using System.Text;
using System.Web.Script.Serialization;

namespace Zquick.Services;

public static class JsonHelper
{
    private static JavaScriptSerializer CreateSerializer()
    {
        return new JavaScriptSerializer
        {
            MaxJsonLength = int.MaxValue,
            RecursionLimit = 256
        };
    }

    public static string Serialize<T>(T value)
    {
        string compact = CreateSerializer().Serialize(value);
        return FormatJson(compact);
    }

    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return CreateSerializer().Deserialize<T>(json);
    }

    private static string FormatJson(string json)
    {
        var sb = new StringBuilder(json.Length * 2);
        int indent = 0;
        bool inQuotes = false;
        bool escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            if (escaped)
            {
                sb.Append(ch);
                escaped = false;
                continue;
            }

            if (ch == '\\')
            {
                sb.Append(ch);
                if (inQuotes)
                {
                    escaped = true;
                }
                continue;
            }

            if (ch == '"')
            {
                sb.Append(ch);
                inQuotes = !inQuotes;
                continue;
            }

            if (inQuotes)
            {
                sb.Append(ch);
                continue;
            }

            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (i + 1 < json.Length && ((ch == '{' && json[i + 1] == '}') || (ch == '[' && json[i + 1] == ']')))
                    {
                        sb.Append(json[++i]);
                    }
                    else
                    {
                        indent++;
                        sb.AppendLine();
                        sb.Append(' ', indent * 2);
                    }
                    break;

                case '}':
                case ']':
                    indent = Math.Max(0, indent - 1);
                    sb.AppendLine();
                    sb.Append(' ', indent * 2);
                    sb.Append(ch);
                    break;

                case ',':
                    sb.Append(',');
                    sb.AppendLine();
                    sb.Append(' ', indent * 2);
                    break;

                case ':':
                    sb.Append(": ");
                    break;

                default:
                    if (!char.IsWhiteSpace(ch))
                    {
                        sb.Append(ch);
                    }
                    break;
            }
        }

        return sb.ToString();
    }
}
