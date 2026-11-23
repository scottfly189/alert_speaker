# alert_speaker

本机喇叭，一条 HTTP，电脑当刻说话。

预警出声，不用盯着聊天窗口。  
只守 127.0.0.1，带鉴权，喇叭不出局域网。  
托盘常驻，音色可换，测试窗口可试听。

alert_speaker 是一个本机常驻的语音播报小服务。你的 Agent 或脚本向 http://127.0.0.1:18765/alert 发一句中文，电脑就会用通义千问 TTS 把警告念出来。它只监听本机、请求要带令牌，可以换音色、可以在测试窗口里先听一遍。也可供 Agent / 本地自动化呼叫。

本文说明如何启动服务、如何配置，以及如何调用 HTTP 接口。调用方只需本机可访问的 HTTP，不必与其它桌面程序同进程。

## 启动方法与常驻方式

在本目录编译并启动：

```bat
dotnet build src\AlertSpeaker.csproj -c Release
dotnet run --project src\AlertSpeaker.csproj -c Release
```

也可以直接运行输出目录中的 `alert_speaker.exe`（通常在 `src\bin\Release\net8.0-windows\`）。

启动后进程进入托盘常驻：关闭主窗口只会隐藏窗口，不会退出；在托盘图标上双击可重新打开主窗口；选择托盘菜单「退出」才结束进程。首次运行若还没有 `App.json`，程序会在运行目录自动生成一份（AlertToken 默认为空、空的 API Key、默认音色），请自行填入通义千问 API Key 后保存。AlertToken 为空时调用 `/alert` 无需携带 `X-Alert-Token`，便于用 Swagger 直接试接口；若已填写令牌，则必须在请求头 `X-Alert-Token` 中携带该令牌。当前如何鉴权、测试时如何带令牌也可在程序「关于」窗口查看；若已设置令牌，关于页会显示当前令牌值，便于粘贴到 Swagger。

若 `127.0.0.1:18765` 已经在监听，或再启动时提示「alert_speaker 已在运行」，说明进程已常驻，不要重复启动。

## 监听地址与文档

- 监听地址（固定，不可改）：`127.0.0.1:18765`
- 只绑定本机回环地址，不监听 `0.0.0.0`，不对外网开放，也不使用内网穿透
- Swagger：http://127.0.0.1:18765/swagger

## App.json

配置文件位于程序运行目录（与 `alert_speaker.exe` 同级）的 `App.json`。该文件已被本目录 `.gitignore` 忽略，不要把真实密钥提交到版本库。字段名如下（示例见同目录 `App.json.example`，仅占位，不含真实密钥）：

| 字段 | 含义 |
| --- | --- |
| `QwenApiKey` | 通义千问 API Key。未配置时，测试窗口会提示，`/alert` 返回 503，进程不会退出。 |
| `AlertToken` | 调用 `/alert` 的鉴权令牌。为空或全空白时不要求鉴权，无头或空头也可调用。已填写时，必须在请求头 `X-Alert-Token` 中携带该令牌。 |
| `Voice` | 当前音色 Id。默认 `longanhuan_v3.1`（龙安欢（女性），支持多方言多语言）。必须是内置系统音色表中的 Id。 |
| `StartWithWindows` | 是否在当前用户登录 Windows 时自动启动。默认 `false`。缺失按 `false`。 |

主窗口提供「开机时自动启动」复选框，默认关闭。勾选后立即把 `StartWithWindows` 写入 `App.json`，并为当前用户登记开机启动；取消勾选则立即写入 `false` 并取消登记。不必先点「保存配置」。

### 如何更换音色

1. 在主窗口「音色」下拉框中选择一项，程序会立即把所选 Id 写入 `App.json` 的 `Voice` 字段。
2. 也可以直接编辑 `App.json` 的 `Voice` 后，在主窗口点击「保存配置」，或重启程序。

切换后，测试窗口试听和 HTTP `/alert` 都会按当前音色播报。音色表与通义千问 `qwen-audio-3.1-tts-flash` 系统音色一致，不要填表中不存在的 Id。

## HTTP 契约

- 方法：`GET` 与 `POST`
- 路径：`/alert`
- 鉴权：`AlertToken` 为空或全空白时不要求鉴权，无 `X-Alert-Token` 头或空头也可调用 `/alert`。若已填写令牌，则只认请求头 `X-Alert-Token`（只此一种，不要改用其它头或 query）；错或缺令牌返回 401，并且不播报。
- 文本：
  - `GET /alert?text=...`（适合短文本）
  - `POST` 建议 JSON body `{"text":"..."}`；也可使用同名 query `text`，适合较长文本
- `text` 缺省或全是空白时返回 400，并且不播报
- `QwenApiKey` 未配置时返回 503，body 说明未配置 Key，并且不播报
- 成功接单后立即返回 `202 Accepted`，再异步做 TTS 和播放，避免 HTTP 卡在播音上。同时多条警告按队列顺序播，不会叠音

### GET 示例

已设置令牌时：

```bat
curl -s -D - -o - -H "X-Alert-Token: YOUR_ALERT_TOKEN" "http://127.0.0.1:18765/alert?text=仓库构建失败，请立刻查看"
```

未设置令牌时，去掉 `-H "X-Alert-Token: YOUR_ALERT_TOKEN"` 即可。

### POST 示例

已设置令牌时：

```bat
curl -s -D - -o - -H "X-Alert-Token: YOUR_ALERT_TOKEN" -H "Content-Type: application/json" -d "{"text":"仓库构建失败，请立刻查看"}" http://127.0.0.1:18765/alert
```

未设置令牌时，去掉 `-H "X-Alert-Token: YOUR_ALERT_TOKEN"` 即可。

成功时响应类似：

```json
{"accepted":true,"status":"queued"}
```

未设置令牌时无需带头即可调用。已设置令牌时，未带令牌或令牌错误：HTTP 401。空文本：HTTP 400。未配置 API Key：HTTP 503。

## 测试窗口

主窗口可填写通义千问 API Key、查看或更新令牌、切换音色，并在输入框中输入中文后点击「试听」。未配置 Key 时会弹出明确提示，不会退出进程。API Key 与鉴权令牌输入框默认显示为密文，点击右侧眼睛图标可切换明文/密文。
