# 开发

## 架构

### 解决方案结构

解决方案主要结构为项目框架-模块的形式，各模块名称均为`ArchiveMaster.Module.*`，独立编译成dll，然后由`ArchiveMaster.UI`进行反射调用。这样做的目的是后续可以开放接口，不改动原始程序而进行开发，灵活加载新模块。

| 项目名称                 | 类型     | 描述                                                         | 依赖                 |
| ------------------------ | -------- | ------------------------------------------------------------ | -------------------- |
| `ArchiveMaster.Core`     | 依赖编译 | 同时被`.UI`和`.Module.*`调用，包含一些基础的接口、基类、配置约定等 | `FzLib`              |
| `ArchiveMaster.UI`       | 依赖编译 | 界面管理程序                                                 | `ArchiveMaster.Core` |
| `ArchiveMaster.UI.*`     | 启动模块 | 具体平台的启动器                                             | `ArchiveMaster.UI`   |
| `ArchiveMaster.Module.*` | 独立编译 | 每个模块在界面上显示为一个组别，同一类的工具放在同一个模块中 | `ArchiveMaster.Core` |

### 项目内部结构

除了`ArchiveMaster.UI.*`外，其余项目结构基本一致。本解决方案的主要结构是总（公共方法、接口、定义）-分（功能模块）-总（UI启动器）

| 项目名称     | 描述                                                         |
| ------------ | ------------------------------------------------------------ |
| `Assets`     | 图标等素材文件，作为`AvaloniaResource`                       |
| `Configs`    | 工具的配置文件                                               |
| `Converters` | 用于XAML的值转换器                                           |
| `Enums`      | 枚举类型                                                     |
| `Messages`   | 用于ViewModel和View之间通过`WeakReferenceMessenger`的通信    |
| `Services`   | 各工具的执行逻辑代码，每个`Service`拥有一个`ConfigBase`的属性。 |
| `ViewModels` | 视图模型，连接`Views`、`Configs`和`Services`。               |
| `Views`      | UI视图界面。本软件实现了完全的MVVM。除`UI`项目外，`Views`中仅包含界面，不包含逻辑。 |

## 模块

### 新增模块

一个模块表现为一个`dll`。步骤如下：

1. 创建一个项目（或复制已有项目并清空），名称前缀必须为`ArchiveMaster.Module.`，`TargetFramework`为`net8.0`，`RootNamespace`为`ArchiveMaster`
2. 新增并实现一个或多个工具
3. 新建一个类，实现`IModuleInfo`，声明模块基本信息

### 新增工具

一个工具，在界面上表现为主页上的一个按钮，在实现中表现为一组同前缀的View、ViewModel、Service、Config。一般来说，步骤如下：

1. 创建一个配置类，继承并实现`ConfigBase`，用于保存配置
2. 创建一个服务类，继承并实现`ServiceBase`，用于工具的具体逻辑实现。大多数工具可以分为初始化和执行两步，这类工具可以继承并实现`TwoStepServiceBase`，实现`InitializeAsync`和`ExecuteAsync`时，应确保不会占用长期主线程。
3. 创建一个视图模型类，继承并实现`ViewModelBase`，用于页面的模型。大多数工具可以分为初始化和执行两步，这类工具可以继承并实现`TwoStepViewModelBase`。
4. 创建一个视图类，继承`PanelBase`，用于页面的模型。大多数工具可以分为初始化和执行两步，这类工具可以继承`TwoStepViewModelBase`。
5. 在实现`IModuleInfo`的类中更新工具相关信息