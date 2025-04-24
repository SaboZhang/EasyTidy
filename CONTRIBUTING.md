# 贡献指南 / Contributing Guide

欢迎加入 **EasyTidy** 社区！无论是提交 Bug、改进文档、贡献新特性，或是提出宝贵建议，都是对项目的巨大帮助。请在参与前阅读并遵循以下流程。

## 📝 行为准则

本项目遵循 [Contributor Covenant](https://www.contributor-covenant.org/) 行为准则。请以友善、尊重的态度参与社区互动。

## 1. 环境准备

1. 克隆仓库  
   ```bash
   git clone https://github.com/SaboZhang/EasyTidy.git
   cd EasyTidy
   ```

2. 安装依赖  
   - **Windows 10/11**  
     - .NET 8 SDK 或更高  
     - Visual Studio 2022（含 WinUI 组件）或 VS Code + C# 扩展  
   - 可选：启用 **Dependabot** 进行依赖安全扫描

3. 运行示例  
   ```bash
   dotnet build
   dotnet run --project src/EasyTidy
   ```

## 2. 提交 Issue

- 确认 Issue 未被提出 → 搜索现有 Issues/Discussions  
- 使用模板，提供 **版本、复现步骤、期望结果、实际结果**  
- 标签：`bug` / `feature` / `documentation` 等

## 3. 开发流程

1. **Fork** 本仓库并克隆到本地  
2. 创建分支  
   ```bash
   git checkout -b feat/xxx   # 或 fix/xxx
   ```
3. 保持分支与 `upstream/main` 同步  
4. 按需编写/更新单元测试  
5. 运行 `dotnet test` 确保全部通过  
6. 提交前执行代码格式化  
   ```bash
   dotnet format
   ```
7. 提交信息遵循 [Conventional Commits](https://www.conventionalcommits.org/zh-hans/v1.0.0/)  
8. Push 并发起 **Pull Request**，勾选 “Allow edits from maintainers”  
9. 等待 CI & Code Review，必要时根据评论修改

## 4. 代码规范

- 使用 **C# 12** 特性需同时兼容 LTS 版本 .NET  
- 遵循 **Microsoft C# Style Guide**（或项目内 `.editorconfig`）  
- 公共 API 需补充 XML Doc 注释  
- 对于核心逻辑，请附带单元/集成测试

## 5. 文档与本地化

- 文档位于 `/docs`，使用 Markdown  
- 中文/英文版皆可，保持文件命名一致（如 `getting-started.zh.md`）

## 6. 发布流程（维护者）

- 合并 PR → `main`  
- 变更日志：更新 `CHANGELOG.md`  
- 标签版本：`git tag vX.Y.Z`  
- GitHub Actions 自动生成 Release

## 7. 致谢

感谢每一位贡献者！您的 GitHub ID 将显示在 [Contributors](https://github.com/SaboZhang/EasyTidy/graphs/contributors) 列表。大型贡献会在 Release Notes 中特别鸣谢。

---

> **提示**：首次参与开源？可阅读《[Git 及 GitHub 工作流快速入门](https://docs.github.com/en/get-started/quickstart)》。有任何疑问，欢迎在 Discussions 区留言！
