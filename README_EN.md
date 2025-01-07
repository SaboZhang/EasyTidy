<p align="center">
<a href="https://github.com/SaboZhang/EasyTidy" target="_blank">
<img align="center" alt="EasyTidy" width="140" src="src/EasyTidy/Assets/icon.png" />
</a>
</p>
<p align="center">
<a href="https://github.com/SaboZhang/EasyTidy/blob/main/LICENSE" target="_self"></a>
<h1 align="center">EasyTidy</h1>
<p align="center">EasyTidy A simple file auto-classification tool makes it easy to create automatic workflows with files
</p>

**English** | [**简体中文**](./README.md)

EasyTidy is a powerful file management software. It can automatically process and organize files and folders, leaving your file system in an orderly manner. Features include: Powerful file filtering: You can filter files by file name, directory, size, date, attributes, content or regular expressions to accurately locate the files you need. Flexible execution mode: Supports manual operation and automatic execution. In terms of automatic execution, you can set a delay start, or you can execute it at fixed intervals, and you can also customize the execution cycle through the CRON expression to meet the file management needs in different scenarios.

Quick start：[Documentation](https://easytidy.luckyits.com)

Technology used: C# .net8 winui3 implementation

TODO:

1.界面设计

- [x] 常规页面设计
- [x] 过滤器界面 + 列表界面
- [x] 任务编排界面 + 列表界面
- [x] 自动化界面
- [x] 设置页面 + 多语言切换
- [x] 错误验证
- [ ] 本地化 国际化(暂未完全实现) 目前支持 简体中文、繁体中文、英文、日语、法语

2.功能实现

- [x] 添加过滤器
- [x] 添加编排任务 + 筛选
- [x] 选择已有分组
- [x] 任务关联过滤器
- [x] 系统托盘图标功能
- [x] 根据配置执行对应操作
- [ ] 备份还原 本地+WebDav (进行中) 已完成本地、webdav备份
- [x] 规则示例
- [x] 筛选器
- [x] 按计划执行
- [x] 定时执行

3.后续计划

- [x] 增加解压操作
- [x] 增加文件WebDav备份
- [x] 前端界面显示Log
- [ ] [#25](https://github.com/SaboZhang/EasyTidy/issues/25)相关问题 (已完成部分功能)
- [x] 移除WinUICommunity相关依赖，作者已停止维护
- [ ] 文件加密
- [ ] 完善文档内容
- [ ] 想到再添加吧！！！
