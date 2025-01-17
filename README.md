<p align="center">
<a href="https://github.com/SaboZhang/EasyTidy" target="_blank">
<img align="center" alt="EasyTidy" width="140" src="src/EasyTidy/Assets/icon.png" />
</a>
</p>
<p align="center">
<a href="https://github.com/SaboZhang/EasyTidy/blob/main/LICENSE" target="_self"></a>
<h1 align="center">EasyTidy</h1>
<p align="center">EasyTidy 一个<strong>简单</strong>的文件<strong>自动分类整理</strong>工具,轻松创建文件的自动工作流程
</p>

[**English**](./README_EN.md) | **简体中文**

EasyTidy 是一款功能强大的文件管理软件，致力于自动化处理和组织文件与文件夹，使您的文件系统保持整洁有序。其主要特色功能包括：

- **强大的文件过滤功能**：支持根据文件名称、所在目录、大小、日期、属性、内容以及常规表达式精准筛选文件，帮助您高效定位所需文件。
- **灵活的执行模式**：不仅支持手动操作，还能自动执行。您可以设置延时启动、定时执行或通过 CRON 表达式自定义执行周期，满足不同场景下的文件管理需求。
- **开源且免费**：完全免费且开源，欢迎大家参与并贡献代码。

### 快速开始

[使用文档](https://easytidy.luckyits.com)

如果您觉得 DropIT 的任务监控无法满足需求，或 File Juggler 3 的定价不符合预算，不妨尝试 EasyTidy。目前，EasyTidy 正在积极开发中，欢迎提出您的需求，我会考虑并接受合理的功能请求。

### 使用技术

- C#
- .NET 8
- WinUI 3

## 关于 License

本项目整体采用MIT协议进行授权。对于项目中包含的 **Snap2HTML** 快照HTML模板，其单独遵循GPL协议。因此，如果您选择使用 **Snap2HTML** 提供的HTML模板，那么必须同时遵守该模板的GPL协议条款。反之，如果不涉及使用 **Snap2HTML** 的HTML模板，整个项目的使用仅需遵循MIT协议即可。

## 功能列表

- [x] 文件移动
- [x] 文件复制
- [x] 删除
- [x] 回收站
- [x] 重命名
- [x] 自动解压
- [x] 压缩文件
- [x] 上传至WebDAV
- [x] 文件加密
- [x] 任务拖拽优先级排序
- [x] 备份
- [x] 硬连接
- [x] 符号连接（软连接）
- [x] 文件快照
- [x] 根据CRON自动执行
- [x] 启动时执行
- [x] 定期执行
- [x] 按照计划执行
- [x] 关闭时执行
- [ ] 本地化支持；目前支持**简体中文**、**繁體中文**、**English**、**日本語**、**Français**；欢迎大家参与并贡献其他语种的翻译。[本地化语言文件](https://github.com/SaboZhang/EasyTidy/tree/main/src/EasyTidy/MultilingualResources)

## 功能规划

### 短期目标 (接下来三个月)

- [ ] 实现右键菜单功能，通过右键执行触发文件整理
- [ ] 文件窗口拖拽
- [ ] 引入AI文章总结功能
- [ ] 文件内容追加到指定文件
- [ ] 效率优化

### 中期目标

- [ ] 探索更多效率执行方式
- [ ] 云盘上传？（待规划）
- [ ] WebDAV 下载

### 长期目标

- [ ] 探索AI接入执行分类的可行性（现阶段AI分类不太符合预期效果）

## 社区参与

欢迎所有人的反馈和贡献！如果你有任何建议或想要帮忙，请访问我们的 [问题跟踪器](https://github.com/SaboZhang/EasyTidy/issues) 也可通过邮件与我联系<service@luckyits.com> 或者 <tao993859833@Live.cn>
