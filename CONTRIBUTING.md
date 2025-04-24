# 贡献指南 / Contributing Guide

> _English text is authoritative; Chinese translation is provided for convenience._  
> _英文为正式文本，中文为参考译文。_

---

## 📝 Code of Conduct  
## 行为准则

This project follows the [Contributor Covenant](https://www.contributor-covenant.org/) Code of Conduct. Please be friendly and respectful in all interactions.  
本项目遵循 [Contributor Covenant](https://www.contributor-covenant.org/) 行为准则。请以友善、尊重的态度参与社区互动。

---

## 1. Getting Started / 环境准备

1. **Clone the repo** / 克隆仓库  
   ```bash
   git clone https://github.com/SaboZhang/EasyTidy.git
   cd EasyTidy
   ```

2. **Install dependencies** / 安装依赖  
   - **Windows 10/11**  
     - .NET 8 SDK or newer / .NET 8 SDK 或更高  
     - Visual Studio 2022 (WinUI workload) or VS Code + C# extension  
   - Optional: enable **Dependabot** for security updates / 可选：启用 **Dependabot** 做依赖安全扫描

3. **Run a sample build** / 运行示例  
   ```bash
   dotnet build
   dotnet run --project src/EasyTidy
   ```

---

## 2. Opening an Issue / 提交 Issue

- Check that the issue hasn’t already been filed (Issues/Discussions search).  
  确认问题未被提出 → 搜索现有 Issues/Discussions  
- Use the template and include **version, repro steps, expected vs. actual behavior**.  
  使用模板，提供 **版本、复现步骤、期望结果、实际结果**  
- Label appropriately: `bug`, `feature`, `documentation`, etc.  
  选择合适标签：`bug` / `feature` / `documentation` …

---

## 3. Development Flow / 开发流程

1. **Fork** the repo and clone it locally / Fork 并克隆到本地  
2. **Create a branch** / 创建分支  
   ```bash
   git checkout -b feat/your-feature        # or fix/your-bug
   ```
3. Keep your branch in sync with `upstream/main`.  
   保持分支与 `upstream/main` 同步  
4. Add or update unit tests where needed.  
   按需编写 / 更新单元测试  
5. Run all tests:  
   ```bash
   dotnet test
   ```
6. Run formatter before committing:  
   ```bash
   dotnet format
   ```
7. Follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) for commit messages.  
   提交信息遵循 **Conventional Commits** 规范  
8. Push and open a **Pull Request**, allowing edits from maintainers.  
   Push 并发起 **Pull Request**，勾选 “Allow edits from maintainers”  
9. Address CI failures or review comments.  
   处理 CI 报错或 Review 评论

---

## 4. Coding Standards / 代码规范

- Use **C# 12** features while remaining compatible with the LTS .NET release.  
  可使用 **C# 12**，同时保持对 LTS 版 .NET 的兼容  
- Follow the **Microsoft C# Style Guide** (or project `.editorconfig`).  
  遵循 **Microsoft C# Style Guide** / `.editorconfig`  
- Public APIs require XML doc comments.  
  公共 API 需补充 XML Doc 注释  
- Core logic must have unit/integration tests.  
  核心逻辑需附带单元 / 集成测试

---

## 5. Docs & Localization / 文档与本地化

- Docs live in [EasyTidy-doc](https://github.com/SaboZhang/EasyTidy-doc) and use Markdown.  
  文档位于 [EasyTidy-doc](https://github.com/SaboZhang/EasyTidy-doc)，使用 Markdown  
- Keep file names in sync across languages (e.g., `getting-started.zh.md`).  
  中英文文件命名保持一致，如 `getting-started.zh.md`

---

## 6. Release Process (Maintainers)  
## 发布流程（维护者）

1. Merge PRs into `main`.  
2. Update `CHANGELOG.md`.  
3. Tag version:  
   ```bash
   git tag vX.Y.Z
   git push --tags
   ```
4. GitHub Actions builds artifacts and creates the Release.  

---

## 7. Acknowledgments / 致谢

Thank you to every contributor! Your GitHub ID appears in the [Contributors](https://github.com/SaboZhang/EasyTidy/graphs/contributors) list. Major contributions are highlighted in release notes.  
感谢每一位贡献者！您的 GitHub ID 将显示在 [Contributors](https://github.com/SaboZhang/EasyTidy/graphs/contributors) 列表，重要贡献会在 Release Notes 中特别鸣谢。

---

> **Tip**: New to open source? See the “[Git & GitHub quickstart](https://docs.github.com/en/get-started/quickstart)” guide. Questions? Ask in Discussions!  
> **提示**：首次参与开源？可阅读《[Git 及 GitHub 工作流快速入门](https://docs.github.com/en/get-started/quickstart)》。有疑问欢迎在 Discussions 区留言！
