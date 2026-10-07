# 第三方素材与制作来源

本项目包含团队创作的内容、第三方素材，以及使用生成式 AI 制作的部分图像。本清单记录直接引入的第三方视觉、音乐及音效素材，不将它们的许可证扩展到整个项目代码或团队原创内容。

以下作者与许可信息于 **2026-10-07** 核对来源页面。Unity、Packages 和 TextMesh Pro 附带内容仍适用各自的许可证；请保留其原始授权文件。

## 图像与 UI

### PSX Decals Free

- 作者：heyheythere。
- 使用文件：`Assets/_Projects/Art/Blocks/PSXDecals/blood_handprint_1.png`。
- 许可：[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/)。
- 来源：<https://heyheythere.itch.io/psx-decals-free>。
- 使用方式：作为第三幕墙面血手印，调整显示大小、位置和透明度。
- 原始授权文件：`Assets/_Projects/Art/Blocks/PSXDecals/LICENSE.txt`；副本见 [ThirdPartyLicenses/PSXDecals-LICENSE.txt](ThirdPartyLicenses/PSXDecals-LICENSE.txt)。
- 游戏内 Staff 页面保留以下署名与许可链接：

  > PSX Decals Free by heyheythere - https://heyheythere.itch.io/psx-decals-free - CC BY 4.0
  >
  > https://creativecommons.org/licenses/by/4.0/

### Complete and Free PSX UI

- 作者：ROHHSA。
- 使用文件：`Assets/_Projects/Art/UI/FreePSXUI/Panel.png`、`ButtonSelected.png`。
- 许可：[CC0 1.0 Universal](https://creativecommons.org/publicdomain/zero/1.0/)，来源页面明确列出该许可。
- 来源：<https://rohhsa.itch.io/free-psx-ui>。
- 修改：从原资源包选取空白面板，将选择框与面板组合为选中按钮图片；文字由游戏 UI 单独绘制。
- 保留原始 `README.txt`，副本见 [ThirdPartyLicenses/FreePSXUI-README.txt](ThirdPartyLicenses/FreePSXUI-README.txt)。

## 音乐与音效

以下六项来源页面均标明 **CC0**，无需强制署名。这里保留作者与来源记录。许可文本见 [ThirdPartyLicenses/CC0-1.0.txt](ThirdPartyLicenses/CC0-1.0.txt)。项目中部分音频转换为 OGG，以减小文件体积。

| 名称 | 作者 / 发布者 | 工程文件 | 来源 |
| --- | --- | --- | --- |
| First Light Particles | Yoiyami | `Audio/BGM/first_light_particles.ogg` | [OpenGameArt](https://opengameart.org/content/first-light-particles-%E2%80%93-cc0-atmospheric-pianoambient-track) |
| Emotional Piano | Centurion_of_war | `Audio/BGM/emotional_piano.ogg` | [OpenGameArt](https://opengameart.org/content/emotional-piano-0) |
| Horror Atmosphere / Post Apocalyptic Wastelands | Juhani Junkala；页面发布账号 SubspaceAudio | `Audio/BGM/Juhani Junkala - Post Apocalyptic Wastelands [Loop Ready].ogg` | [OpenGameArt](https://opengameart.org/content/horror-atmosphere) |
| Pursuit / Vicegrip of Pursuit | Sudocolon | `Audio/BGM/Vicegrip of Pursuit.mp3` | [OpenGameArt](https://opengameart.org/content/pursuit) |
| Ticking Clock | AntumDeluge | `Audio/BGM/ticking_clock.ogg` | [OpenGameArt](https://opengameart.org/content/ticking-clock-0) |
| 8 wet squish, slurp impacts | Independent.nu；提交者 qubodup | `Audio/SFX/impactsplat03.mp3` | [OpenGameArt](https://opengameart.org/content/8-wet-squish-slurp-impacts) |

表中工程文件路径均相对于 `Assets/_Projects/`。滴答声音效原附的 `ticking_clock_LICENSE.txt` 仍保留在资源目录中。

## 生成式 AI 图像

据团队提供的制作来源，下列图片使用 **GPT Image 2.5** 生成：

- `Assets/_Projects/Art/CG/夕阳下的温馨像素卧室.png`
- `Assets/_Projects/Art/CG/深夜月光下的焦虑等待.png`
- `Assets/_Projects/Art/CG/月光下的悲伤拥抱.png`
- `Assets/_Projects/Art/UI/MainMenuCover.png`

此处为制作来源披露，不将上述图片标为 CC0，也不将团队绘制的角色、家具、地图等素材标为 AI 生成。参加活动或发布作品时，应同时遵守平台的 AI 使用披露要求。

## 字体：待进一步核对

`Assets/_Projects/Fonts/CJK SDF.asset` 的源字体信息为 Microsoft YaHei，源路径为 `C:/Windows/Fonts/msyh.ttc`。当前仓库没有提交该字体原文件，但 TMP Font Asset 包含生成的字体图集，并使用系统字体动态生成模式。

**没有提交原字体文件，不等同于字体图集的分发授权已经核实。** 本次整理没有为微软雅黑宣称开源许可，也没有替换现有 UI 字体。后续应核对适用字体许可，或换成许可明确的中文字体，并验证中文字符与 UI 排版。

## 已从当前版本移除的音乐

《砂の雫》（Tears of Sand），作者秋山裕和，来源 [H/MIX GALLERY](https://www.hmix.net/)，适用 [H/MIX 使用条款](https://www.hmix.net/terms.html)。官网允许个人、同人游戏中的普通 BGM 使用，同时禁止单独再分发音乐文件。

为避免公开源码仓库提供独立音频下载，当前版本已移除 `砂の雫.ogg` 和对应 `.meta`，主菜单改用 First Light Particles。原曲仅在开发者仓库外保留私人备份。

**此变更没有重写 Git 历史。** 旧提交以及既有 `v1.1.0` 标签中的原文件仍然存在；历史与旧标签的清理需要另行协调，不属于本次普通提交已经解决的内容。
