# WordIO

WordIO 是一个零外部依赖的 C#/.NET 10 DOCX 文本读取示例，包含两个项目：

- `WordIO.Core`：核心文档模型和简易 XML Parser。
- `WordIO.Mini`：仅解析 DOCX 包中的文本内容，并生成核心模型；支持 Native AOT 发布。

## 项目结构

```text
WordIO.slnx
src/
  WordIO.Core/
    Models/           Document、Section、Paragraph、Run、Table、List、OrderedList 等
    Xml/              简易 XML Parser
  WordIO.Mini/
    DocxTextReader.cs DOCX 读取入口
    Program.cs        命令行入口
```

## 构建

```powershell
dotnet build WordIO.slnx -c Release
```

## 运行

```powershell
dotnet run --project src/WordIO.Mini/WordIO.Mini.csproj -c Release -- path\to\document.docx
```

## Native AOT 发布

```powershell
dotnet publish src/WordIO.Mini/WordIO.Mini.csproj -c Release -r win-x64 -o artifacts/publish
.\artifacts\publish\WordIO.Mini.exe path\to\document.docx
```

当前 `WordIO.Mini` 已设置 `PublishAot=true`、`InvariantGlobalization=true`，并保持零 NuGet 包依赖。

## API

```csharp
using WordIO.Mini;

var document = DocxTextReader.Read(@"document.docx");
Console.WriteLine(document.Text);
```

`Document.Text` 会按段落、列表项和表格输出纯文本；模型对象仍保留 `Paragraph`、`Run`、`Table`、`List`、`OrderedList` 等结构，可用于后续处理。
