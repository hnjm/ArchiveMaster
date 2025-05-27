# 文件加密解密

## 功能特性

- 使用AES加密算法，对文件进行加密解密
- 除了支持对文件内容加密外，还支持对文件名、文件夹名或整体目录结构进行加密
- 支持选择算法的填充模式、算法类型

## 配置

1. **未加密目录/加密后目录**：加密前后的目录。注意，加密操作时，将地区未加密目录，写入加密后目录；解密操作时，将读取加密后目录，写入未加密目录
2. **密码**：加密或解密的密码。密码最小长度为1位，最大长度为32

# 批量重命名

## 功能特性

- 支持文件和文件夹两种目标类型的重命名
- 提供多种搜索模式：包含、匹配扩展名、匹配文件名、匹配全名、正则表达式
- 提供多种重命名模式：替换关键词、替换扩展名、替换文件名、替换全名、保留匹配值、保留匹配值和扩展名、C#脚本高级模式
- 支持忽略大小写选项
- 自动处理重命名冲突，确保不会覆盖现有文件
- 提供C#脚本支持实现高级重命名逻辑

## 配置

1. **目录**：选择要进行重命名操作的根目录
2. **搜索关键词**：输入用于匹配文件/文件夹名的关键词或正则表达式
3. **替换关键词**：输入用于替换匹配部分的内容或C#脚本代码
4. **搜索选项**：
   - **类型**：选择要重命名的目标是文件还是文件夹
   - **搜索模式**：选择搜索匹配方式（包含、匹配扩展名等）
   - **包括子目录**：是否包含子目录中的文件/文件夹
   - **忽略大小写**：是否忽略大小写进行匹配
5. **重命名选项**：
   - **重命名模式**：选择重命名方式（替换关键词、替换扩展名等）

## 搜索模式

| 名称       | 说明                                | 示例                                                         |
| ---------- | ----------------------------------- | ------------------------------------------------------------ |
| 包含       | 检查文件名/路径是否包含指定的关键词 | 搜索词：`report`<br>匹配：`report.pdf`, `annual_report.docx`<br>不匹配：`data.txt`, `notes.doc` |
| 匹配扩展名 | 精确匹配文件扩展名（不含点）        | 搜索词：`pdf`<br>匹配：`doc.pdf`, `presentation.PDF`<br>不匹配：`image.png`, `data.xlsx` |
| 匹配文件名 | 精确匹配文件名部分（不含扩展名）    | 搜索词：`invoice`<br>匹配：`invoice.pdf`, `INVOICE.xlsx`<br>不匹配：`invoice_2023.doc`, `old_invoice.txt` |
| 匹配全名   | 精确匹配完整文件名（含扩展名）      | 搜索词：`readme.txt`<br>匹配：`readme.txt`, `README.TXT`<br>不匹配：`readme.md`, `readme.txt.bak` |
| 正则表达式 | 使用正则表达式进行高级匹配          | 搜索词：`^img_\d{4}\.jpg$`<br>匹配：`img_2023.jpg`, `IMG_0001.JPG`<br>不匹配：`image.jpg`, `img123.png` |

注：表格中所有示例均假设开启了"忽略大小写"选项

## 重命名模式

| 名称               | 说明                         | 示例                                                         |
| ------------------ | ---------------------------- | ------------------------------------------------------------ |
| 替换关键词         | 替换匹配到的部分             | 原文件：`photo_001.jpg`<br>替换为：`image_001.jpg`           |
| 替换扩展名         | 仅替换文件扩展名             | 原文件：`document.doc`<br>替换为：`document.docx`            |
| 替换文件名         | 替换文件名部分（保留扩展名） | 原文件：`old_report.pdf`<br>替换为：`new_report.pdf`         |
| 替换全名           | 完全替换整个文件名           | 原文件：`temp.tmp`<br>替换为：`archive.zip`                  |
| 保留匹配值         | 仅保留匹配部分作为新名称     | 原文件：`project_alpha_v1.zip`<br>匹配`[a-z]+(?=_v)`<br>结果：`alpha.zip` |
| 保留匹配值和扩展名 | 保留匹配部分+原扩展名        | 原文件：`20231115_notes.doc`<br>匹配`\d{8}`<br>结果：`20231115.doc` |
| 高级（C#脚本）     | 使用C#代码自定义重命名逻辑   | 脚本：`return "NEW_" + file.Name;`<br>原文件：`test.txt`<br>结果：`NEW_test.txt` |

除高级（C#脚本），其余，模式均支持占位符，见[文件信息占位符](#文件信息占位符)

注：表格中所有示例均假设开启了"忽略大小写"选项

### 高级（C#脚本）重命名模式

C#脚本模式允许用户使用C#代码自定义文件重命名逻辑，适用于复杂或动态的重命名需求。

在 C# 脚本中，可以直接访问以下全局变量：

| 变量      | 类型             | 说明                                   |
| --------- | ---------------- | -------------------------------------- |
| `file`    | `RenameFileInfo` | 当前文件的信息（路径、名称、扩展名等） |
| `matched` | `string`         | 匹配到的字符串（取决于搜索模式）       |

`RenameFileInfo` 对象提供以下常用属性：

| 属性           | 类型       | 说明                         | 示例                     |
| :------------- | :--------- | :--------------------------- | :----------------------- |
| `Name`         | `string`   | 文件名（包含扩展名）         | `"report.pdf"`           |
| `Path`         | `string`   | 文件的完整绝对路径           | `"C:\Files\report.pdf"`  |
| `RelativePath` | `string`   | 相对于TopDirectory的相对路径 | `"subfolder\report.pdf"` |
| `TopDirectory` | `string`   | 根目录路径                   | `"C:\Files"`             |
| `IsDir`        | `bool`     | 标识是否是文件夹             | `true`（文件夹）         |
| `Length`       | `long`     | 文件大小（字节），文件夹为0  | `1024`（1KB文件）        |
| `Time`         | `DateTime` | 最后修改时间                 | `2023-11-15 14:30:00`    |

代码在“替换关键词”中编写，允许一行或多行，最后一行应当返回一个字符串。例如：

1. 使用哈希值作为文件名：

````csharp
return BitConverter.ToString(MD5.HashData(File.OpenRead(file.Path))).Replace("-", "") + Path.GetExtension(file.Path);
````

2. 移除文件名中的数字：

```csharp
var name = Path.GetFileNameWithoutExtension(file.Name);
var ext = Path.GetExtension(file.Name);
var cleanName = Regex.Replace(name, @"[0-9]", ""); // 移除非字母数字
return cleanName + ext;
```

如：`a1234bcd.ps1`→`abcd.ps1`

3. 根据文件大小分类重命名：

```csharp
var name = Path.GetFileNameWithoutExtension(file.Name);
var ext = Path.GetExtension(file.Name);
var suffix = file.Length < 1024 * 1024 ? "_small" : "_large";
return name + suffix + ext;
```

4. 将形似`aa bb.ext`的文件重命名为`Aa_Bb.ext`：

```csharp
// 获取文件名和扩展名
var nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
var extension = Path.GetExtension(file.Name);

// 分割单词并处理每个单词
var words = nameWithoutExt.Split(' ');
var processedWords = words
    .Where(word => !string.IsNullOrEmpty(word))  // 过滤空字符串
    .Select(word => 
        word.Length > 1 
            ? char.ToUpper(word[0]) + word.Substring(1).ToLower()  // 首字母大写，其余小写
            : word.ToUpper()  // 单字符直接大写
    );

// 用下划线连接并添加扩展名
return string.Join("_", processedWords) + extension;
```

5. 为文件添加序号后缀：

```csharp
static int counter = 1;
return $"file_{counter++:000}{Path.GetExtension(file.Name)}";
```



# 批量命令行执行

本工具可通过配置指定目录、选择命令行程序并设置参数，利用动态占位符（如`<Path>`、`<DirRelPath>`）自动替换文件路径或生成目录结构，实现对文件或目录的批量处理。工具支持多种文件遍历模式（如全量文件、顶层目录、特定层级元素），适用于自动化脚本执行、文件操作、系统管理等场景，显著提升批量任务的开发效率与管理体验。

## 配置

| 配置名       | 描述                                                         |
| :----------- | :----------------------------------------------------------- |
| 目录         | 指定需要处理的根目录路径，工具将在此目录下遍历文件或文件夹。支持通过文件选择器直接选择目录，确保路径的正确性。 |
| 程序         | 设置用于执行命令行的程序，可以是一个普通的支持命令行参数的exe，也可以是系统的shell（例如Windows下的`cmd`和`PowerShell`、Linux和MacOS下的`/bin/bash`）。 |
| 命令行参数   | 定义需要执行的命令行参数，必须提供至少一个占位符（如`<Path>`、`<DirRelPath>`），用于动态替换文件路径、相对路径等信息。 |
| 自动创建目录 | 部分命令运行前需要手动创建目标目录。该参数用于指定需要自动创建的目录路径，支持使用占位符动态生成目录结构。例如，`C:\Temp\<DirRelPath>`会根据文件的相对路径自动创建对应的目录结构，确保命令执行前的目录准备。 |
| 列举对象     | 选择需要处理的目标类型，包括文件、目录或混合元素。支持按层级深度检索（如`C:\*\*`二级目录）。 |

## 例子

假设有一个需求，要将`D:\照片`下的目录进行压缩。压缩时，要求对该目录下的每个目录中的目录单独压缩成一个rar压缩包，放到`D:\output`中，例如：

- `D:\照片\宁波\四明山`目录压缩为`D:\output\宁波\四明山.rar`
- `D:\照片\上海\陆家嘴`目录压缩为`D:\output\上海\陆家嘴.rar`
- ……

那么， 模块应该做如下配置：

| 配置名       | 配置值                                 |
| ------------ | -------------------------------------- |
| 目录         | `D:\照片`                              |
| 程序         | `C:\Program Files\WinRAR\Rar.exe`      |
| 命令行参数   | `a "D:\output\<RelPath>.rar" "<Path>"` |
| 自动创建目录 | `D:\output\<DirRelPath>`               |
| 列举对象     | 指定深度目录，层数为1                  |

该配置表示，会自动枚举`D:\照片`下，`D:\照片\*`的目录，并调用RAR程序，将每个目录压缩到`D:\output`，并保留相对目录结构。压缩前，会创建目标压缩文件所在的目录，防止压缩出错。