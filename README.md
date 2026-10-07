# 莫比乌斯之幕：暮色记忆

## 开发感想

七天的Gamejam，48h的开发时间，说长不长说短不短。结果固然也有很多不完美，来不及实现的点子，彻夜难眠的bug，忍痛删减的内容，但我注定会难以忘记这48h里，与大家一起拼尽全力，为相同目标而奋斗的时光

—— by night

## 游戏简介

《莫比乌斯之幕：暮色记忆》是一款使用 Unity 制作的 2D 俯视角叙事解谜游戏，在 GameJam 期间由团队协作完成。玩家通过房间探索、物品调查与收集，经历「慕、暮、墓、幕」四幕，逐步拼起记忆中的故事。游戏包含追逐段落。

## 运行方式

当前提交平台为 **Windows 64 位**，使用键盘和鼠标游玩。

1. 将 Windows 游戏压缩包完整解压到一个文件夹。
2. 双击 `MobiUs-Veil.exe` 启动，在主菜单选择开始游戏。
3. 请保留程序旁的 `_Data` 文件夹、`UnityPlayer.dll` 等文件，它们是运行所必需的。

## 操作说明

| 操作 | 按键 |
| --- | --- |
| 移动 | W / A / S / D |
| 调查、交互 | E |
| 推进对白 | E / 空格 / 鼠标左键 |
| 打开、关闭背包 | B |
| 游戏菜单、关闭当前菜单 | Esc |
| 点击菜单按钮、选择对话选项 | 鼠标左键 |

## 第三方素材与制作来源

本项目使用团队原创内容及部分第三方素材。作者、来源、许可证及待核对事项详见 [第三方素材说明](THIRD_PARTY_ASSETS.md)，可随游戏包分发的署名文件见 [CREDITS.txt](CREDITS.txt)。

部分 CG 和主菜单封面使用 GPT Image 2.5 生成，具体文件列于上述说明；团队绘制的角色、道具和地图素材不在该 AI 图像清单中。

公开源码中的主菜单默认使用 CC0 素材 First Light Particles。开发者可通过私人素材构建游戏包，在游戏中使用原曲《砂の雫》，但不在当前源码中提供独立音频下载。旧 Git 历史及 v1.1.0 标签的音乐文件尚待另行清理；现有中文字体的分发授权也仍需核对。

构建时，`Tools → 打包 Windows 版` 使用公开源码中的默认音乐；`Tools → 打包 Windows 版（原主菜单音乐）` 从仓库外的 `../PrivateAssets/Hmix/砂の雫.ogg` 载入原曲，只替换构建过程中的主菜单音乐。私人原曲不随源码提供，第二个命令缺少原曲时会报错。命令行入口为 `BuildWindows.BuildWin64WithPrivateMenuMusic`，可用 `-privateMenuMusic` 指定私人音频路径。

---

<details>
<summary>工程与团队协作说明（点击展开）</summary>

Unity 2D 游戏项目，团队协作开发。

- 远端仓库：<https://github.com/shengyejiong/MobiUs-Veil>
- Unity 版本：**6000.3.25f1**（两人必须用同一版本打开，版本不一致会重写场景/资源文件，产生大量无意义冲突）

---

## 一、第一次拉取之后

1. 用 **Unity Hub → Add → 选择本目录**，用 `6000.3.25f1` 打开，等资源导入跑完（会生成 `Library/`，约 2.5 GB，已被 `.gitignore` 忽略，**永远不要提交**）。
2. 配置 Unity 场景合并工具（每台机器只做一次，见第四节）。
3. 确认 `Edit → Project Settings → Editor → Asset Serialization = Force Text`（仓库里已经是这个设置，换机器后确认一下即可）。

## 二、协作铁律

- **开工先 `git pull`，收工就 `git push`。** 不要攒好几天才推一次，拖得越久冲突越多。
- 一次提交只做一件事，写清楚信息：`feat: 新增暂停菜单` / `fix: 修复对话触发` / `chore: 整理资源目录`。
- 不要提交 `Library/ Temp/ Logs/ UserSettings/ Build/ .vscode/`（已在 `.gitignore`）。
- **不要两个人同时改同一个场景或预制体**，改场景前先 pull，改完立刻提交并推送。
- 单个美术/音频文件超过 ~50 MB 时先说一声，改用 Git LFS。

## 三、日常命令

```bash
git pull                      # 开工前
git status                    # 看看自己改了啥
git add <文件>                # 只加这次要提交的
git commit -m "feat: xxx"
git push                      # 收工
```

## 四、场景冲突怎么办（UnityYAMLMerge）

`.unity` / `.prefab` / `.asset` 是 YAML 文本，默认的 Git 合并会把它们搞坏。仓库里的 `.gitattributes` 已经声明这些文件交给 Unity 自带的 **SmartMerge** 处理，只需每台机器执行一次（路径换成自己的 Unity 安装目录）：

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"G:/unityhub/6000.3.25f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

没配置也不会报错，只是退回 Git 默认合并。万一还是冲突：**直接用 Unity 打开场景手动改**，改完 `git add` → `git commit`。

## 五、常见问题

**1. `schannel: failed to receive handshake` / `Failed to connect to github.com port 443 via 127.0.0.1`**

本机网络加速工具打架。记住：**Steam++（Watt Toolkit）的「GitHub 加速」和 Clash 系列只能开一个。**

- Steam++ 会把 `github.com` 写进 hosts 指向 `127.0.0.1`，靠它自己的 `Steam++.Accelerator` 在 443 端口转发；**退出 Steam++ 前要先关掉「GitHub 加速」**，否则 hosts 里的 `127.0.0.1` 不会被清掉，git 会一直连本机 443。
- Clash 会占用系统代理端口（7xxx）并接管 TUN/DNS，和 Steam++ 抢同一段流量。
- 自查三条：

```powershell
Select-String github C:\Windows\System32\drivers\etc\hosts   # 有 127.0.0.1 github.com 就是 Steam++ 在接管
netstat -ano | findstr ":443 :7897"                          # 加速器 / 代理端口活着没
git ls-remote origin                                         # 通路到底行不行
```

**2. `! [rejected] main -> main (non-fast-forward)`**

远端有你没有的提交：`git pull --no-rebase origin main` 然后 `git push origin main`。别用 `-f` 强推，会删掉队友的提交。

**3. 资源丢失引用（粉色 Missing）**

多半是某个资源没入库。在项目根目录执行下面这段，能列出「被引用但没提交」的资源：

```powershell
$ref = git grep -h -o -E 'guid: [0-9a-f]{32}' -- Assets | ForEach-Object { $_ -replace 'guid: ','' } | Sort-Object -Unique
git ls-files --others --exclude-standard -- Assets | Where-Object { $_ -like '*.meta' } | ForEach-Object {
  $g = (Select-String -Path $_ -Pattern '^guid: (\w+)').Matches.Groups[1].Value
  if ($ref -contains $g) { "未入库但被引用 -> $_" }
}
```

</details>
