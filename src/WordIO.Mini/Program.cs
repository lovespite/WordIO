using System.Text;
using WordIO.Mini;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length != 1)
{
    Console.Error.WriteLine("用法: WordIO.Mini <input.docx>");
    return 1;
}

try
{
    var text = DocxTextReader.ExtractText(args[0]);
    Console.WriteLine(text);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"读取 DOCX 失败: {exception.Message}");
    return 1;
}
