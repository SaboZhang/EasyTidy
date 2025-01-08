<p align="center">
<a href="https://github.com/SaboZhang/EasyTidy" target="_blank">
<img align="center" alt="EasyTidy" width="140" src="src/EasyTidy/Assets/icon.png" />
</a>
</p>
<p align="center">
<a href="https://github.com/SaboZhang/EasyTidy/blob/main/LICENSE" target="_self"></a>
<h1 align="center">EasyTidy</h1>
<p align="center">EasyTidy 一个简单的文件自动分类整理工具,轻松创建文件的自动工作流程
</p>

[**English**](./README_EN.md) | **简体中文**

EasyTidy 是一款功能强大的文件管理软件。它能够自动处理和组织文件与文件夹，让你的文件系统变得井井有条。 特色功能包括： 强大的文件过滤：可以依据文件名称、所在目录、大小、日期、属性、内容或者常规表达式来筛选文件，精准定位你所需的文件。 灵活的执行模式：支持手动操作和自动执行。在自动执行方面，你可以设置延时启动，也可以按照固定的时间间隔执行，还能通过 CRON 表达式自定义执行周期，满足不同场景下的文件管理需求。开源且免费

快速开始：[使用文档](https://easytidy.luckyits.com)

如果你觉得 DropIT 的任务监控无法满足需求，或者 File Juggler 3 的价格不太合适，可以尝试一下 EasyTidy。目前 EasyTidy 正在积极开发中，欢迎提出您的需求，我会考虑并接受一些合理的功能请求。

使用技术：C# .net8 winui3 实现

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
- [x] 备份还原 本地+WebDav
- [x] 规则示例
- [x] 筛选器
- [x] 按计划执行
- [x] 定时执行

3.后续计划

- [x] 增加解压操作
- [x] 增加文件WebDav备份
- [x] 前端界面显示Log
- [x] [#25](https://github.com/SaboZhang/EasyTidy/issues/25)相关问题
- [x] 移除WinUICommunity相关依赖，作者已停止维护
- [ ] 文件加密
- [ ] 完善文档内容
- [ ] 想到再添加吧！！！
