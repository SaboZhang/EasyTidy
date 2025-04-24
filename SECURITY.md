# Security Policy  
# 安全策略

> _English text is authoritative; Chinese translation is provided for convenience._  
> _英文为正式文本，中文为参考译文。_

---

## Supported Versions  
## 支持的版本

| Version | Maintained? | Notes | 版本 | 是否维护 | 说明 |
|---------|-------------|-------|------|---------|------|
| `main` branch | ✅ | Always receives security patches | `main` 分支 | ✅ | 始终接收安全补丁 |
| Latest release (e.g., v1.x) | ✅ | Patches provided if users are affected | 最近发布版本 (如 v1.x) | ✅ | 若影响用户，将提供修复补丁 |
| Older releases | ❌ | Not maintained—please upgrade | 历史版本 | ❌ | 不再维护，建议升级 |

> **Note**: Always upgrade to the latest version to receive security fixes and new features.  
> **注意**：请始终使用最新版本，以获得安全更新和最新功能。

---

## How to Report a Vulnerability  
## 如何报告漏洞

**Please do NOT open public Issues or Pull Requests for security problems.**  
Send a private report to:

- 📧 `service@luckyits.com`

Include:

1. Affected version or commit hash  
2. Description and impact of the vulnerability  
3. *Optional*: Reproduction steps or PoC  
4. *Optional*: Your preferred attribution name/team

We commit to:

- **Confirm receipt within 48 hours**  
- **Fix or mitigate within 90 days** (usually sooner)  
- Coordinate public disclosure with you if needed

我们承诺：

- **48 小时内** 确认收到报告  
- **90 天内**（通常更快）完成修复  
- 如需公开披露，将事先与您协调

---

## PGP Public Key (optional)  
## PGP 公钥（可选加密通信）

```text
-----BEGIN PGP PUBLIC KEY BLOCK-----
<Insert your PGP key here, or remove this section>
-----END PGP PUBLIC KEY BLOCK-----
```

---

## What Security Issues We Care About  
## 我们关注的安全问题类型

- Arbitrary file overwrite/deletion or privilege escalation  
- Path traversal and injection  
- AI‑related request forgery or local RCE  
- Leakage of sensitive data via configs/logs  
- Vulnerable third‑party dependencies (e.g., Serilog, AI libraries)  
- Denial‑of‑Service (large‑file loops, resource exhaustion)

由于 EasyTidy 涉及本地文件操作与 AI 集成，我们尤其关注：

- 任意文件覆盖 / 删除 / 提权  
- 路径注入与目录遍历  
- AI 请求伪造 / 本地 RCE  
- 配置或日志泄露敏感信息  
- 第三方依赖漏洞  
- 拒绝服务攻击

---

## Out‑of‑Scope Issues  
## 暂不处理的问题

- Non‑security bugs or performance suggestions  
- Problems caused by user plugins or custom scripts  
- Vulnerabilities fixed in newer versions—please upgrade

---

## Security Process  
## 安全处理流程

1. **Receipt & Acknowledgement** – reply within 48 h  
2. **Assessment** – CVSS or similar scoring  
3. **Fix** – patch in a private branch, verify & test  
4. **Release** – publish version vX.Y.Z & changelog  
5. **Disclosure** – via GitHub Security Advisory & CVE (if assigned)

1. **接收与确认**：48 小时内回复  
2. **漏洞评估**：使用 CVSS 等标准  
3. **修复漏洞**：私有分支修复并测试  
4. **发布补丁**：推送新版本并更新日志  
5. **公开披露**：GitHub Security Advisory / CVE

---

## Acknowledgments  
## 致谢

We thank every user, developer, and researcher who helps keep **EasyTidy** secure!  
感谢所有为 **EasyTidy** 安全性做出贡献的用户、开发者和安全研究人员！

---

_Last updated: 2025‑04‑24_  
_最后更新：2025‑04‑24_
