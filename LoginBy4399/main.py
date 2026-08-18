import wx
import wx.html2
import threading
import json
import uuid
import re
import requests
from curl_cffi import requests as cffi_requests


def get_oauth_url():
    """获取 OAuth 回调初始 URL"""
    session = cffi_requests.Session(impersonate="chrome110")
    session.verify = False
    session.headers = {
        "User-Agent": (
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
            "AppleWebKit/537.36 (KHTML, like Gecko) "
            "Chrome/151.0.0.0 Safari/537.36 Edg/151.0.0.0"
        )
    }

    response = session.get(
        "https://m.4399api.com/openapi/oauth-callback.html"
        "?gamekey=44770&game_key=115716"
    ).json()
    return response["result"]


class BrowserFrame(wx.Frame):
    def __init__(self):
        super().__init__(None, title="4399 SauthJson 提取工具", size=(900, 700))

        self.browser = wx.html2.WebView.New(self)
        self.Show()

        # 绑定导航事件
        self.browser.Bind(wx.html2.EVT_WEBVIEW_NAVIGATING, self.on_navigating)
        self.browser.Bind(wx.html2.EVT_WEBVIEW_NAVIGATED, self.on_navigated)
        self.browser.Bind(wx.html2.EVT_WEBVIEW_LOADED, self.on_loaded)

        self.sauth_processed = False

        threading.Thread(target=self._load_url, daemon=True).start()

    def _load_url(self):
        try:
            url = get_oauth_url()
            wx.CallAfter(self.browser.LoadURL, url)
        except Exception as e:
            wx.CallAfter(self._show_error, str(e))

    def _show_error(self, msg):
        wx.MessageBox(f"获取 URL 失败：\n{msg}", "错误", wx.OK | wx.ICON_ERROR)

    # ---------- 导航事件处理 ----------
    def on_navigating(self, event):
        pass

    def on_navigated(self, event):
        pass

    def on_loaded(self, event):
        if not self.sauth_processed:
            self._try_extract_json_and_process()

    def _run_script(self, script):
        """执行 JavaScript 并正确处理返回值"""
        try:
            result = self.browser.RunScript(script)

            if isinstance(result, tuple):
                if len(result) >= 2:
                    return result[1]
                elif len(result) == 1:
                    return result[0]
                else:
                    return None
            elif isinstance(result, list):
                if len(result) >= 2:
                    return result[1]
                elif len(result) == 1:
                    return result[0]
                else:
                    return None
            elif isinstance(result, str):
                return result
            else:
                return str(result)
        except Exception as e:
            print(f"执行脚本失败: {e}")
            return None

    def _try_extract_json_and_process(self):
        """从网页中提取 JSON 内容并处理"""
        try:
            page_content = self._run_script("document.body.innerText")

            if not page_content or page_content == "True":
                return

            try:
                data = json.loads(page_content)
            except json.JSONDecodeError:
                json_match = re.search(r'\{.*\}', page_content, re.DOTALL)
                if json_match:
                    try:
                        data = json.loads(json_match.group())
                    except:
                        return
                else:
                    return

            result = data.get("result")
            if not result:
                return

            uid = result.get("uid")
            state = result.get("state")

            if uid is None or state is None:
                return

            uid = str(uid)
            state = str(state)

            self.sauth_processed = True
            threading.Thread(target=self._process_sauth, args=(uid, state), daemon=True).start()

        except Exception as e:
            print(f"提取 JSON 时出错: {e}")

    def _process_sauth(self, uid, state):
        """构建 sauth_json、发送请求并注入网页"""
        try:
            # 构建 sauth_json
            sauth = {
                "aim_info": json.dumps(
                    {"aim": "127.0.0.1", "country": "CN", "tz": "+0800", "tzid": ""},
                    separators=(",", ":")
                ),
                "realname": json.dumps(
                    {"realname_type": 2},
                    separators=(",", ":")
                ),
                "app_channel": "4399com",
                "platform": "ad",
                "client_login_sn": "4399_Gen",
                "gameid": "x19",
                "login_channel": "4399com",
                "sdk_version": "3.12.2",
                "sdkuid": uid,
                "sessionid": state,
                "udid": uuid.uuid4().hex[:16],
                "deviceid": "4399_Gen"
            }
            sauth_json = json.dumps(sauth, separators=(",", ":"), ensure_ascii=False)

            # 发送 POST 请求
            headers = {
                "Content-Type": "application/json; charset=utf-8",
                "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/129.0.0.0 Safari/537.36"
            }
            resp = requests.post(
                "https://mgbsdk.matrix.netease.com/x19/sdk/uni_sauth",
                data=sauth_json.encode("utf-8"),
                headers=headers,
                timeout=10,
                allow_redirects=False
            )

            # 这里为了更好的排版，对JSON进行格式化（indent=4）
            parsed_sauth = json.loads(sauth_json)
            formatted_json = json.dumps(
                {"sauth_json": parsed_sauth},
                ensure_ascii=False
            )

            # 注入WinUI风格的网页
            wx.CallAfter(self._inject_response, formatted_json)

        except Exception as e:
            print(f"处理 sauth 失败: {e}")
            wx.CallAfter(self._show_error, str(e))

    def _inject_response(self, response_json):
        """将响应内容注入到 WinUI 3 风格的网页中"""
        try:
            # 转义 JSON 字符串用于 JavaScript
            escaped_json = response_json.replace('\\', '\\\\').replace("'", "\\'").replace('\n', '\\n').replace('\r',
                                                                                                                '\\r')

            html_content = f"""
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
                <meta charset="UTF-8">
                <style>
                    /* 引入 WinUI 3 特色：Segoe UI Variable 和 Fluent Icons */
                    @font-face {{
                        font-family: 'Segoe Fluent Icons';
                        src: local('Segoe Fluent Icons'), local('Segoe MDL2 Assets');
                    }}

                    :root {{
                        --sys-base-high: #1A1A1A;
                        --sys-base-medium: #5F5F5F;
                        --sys-page-bg: #F3F3F3;
                        --sys-card-bg: #FFFFFF;
                        --sys-border: #E5E5E5;
                        --sys-accent: #005FB8;
                        --sys-accent-hover: #0058A8;
                        --sys-accent-active: #00529C;
                        --sys-success-bg: #DFF6DD;
                        --sys-success-text: #0F5207;
                    }}

                    body {{
                        font-family: 'Segoe UI Variable', 'Segoe UI', -apple-system, sans-serif;
                        background-color: var(--sys-page-bg);
                        color: var(--sys-base-high);
                        margin: 0;
                        padding: 32px 40px;
                        -webkit-font-smoothing: antialiased;
                    }}

                    .container {{
                        max-width: 800px;
                        margin: 0 auto;
                    }}

                    /* 标题区域 */
                    .header {{
                        margin-bottom: 28px;
                    }}

                    .title {{
                        font-size: 28px;
                        font-weight: 600;
                        margin: 0 0 8px 0;
                        display: flex;
                        align-items: center;
                        gap: 12px;
                        letter-spacing: -0.5px;
                    }}

                    .icon {{
                        font-family: 'Segoe Fluent Icons';
                        font-weight: normal;
                        font-style: normal;
                    }}

                    .title .icon {{
                        color: #107C10; /* Windows 成功绿 */
                        font-size: 24px;
                    }}

                    .subtitle {{
                        font-size: 14px;
                        color: var(--sys-base-medium);
                        margin: 0;
                    }}

                    /* WinUI 卡片控件 */
                    .card {{
                        background: var(--sys-card-bg);
                        border: 1px solid var(--sys-border);
                        border-radius: 8px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.02), 0 0 2px rgba(0,0,0,0.04);
                        overflow: hidden;
                        margin-bottom: 24px;
                    }}

                    .card-header {{
                        padding: 16px 20px;
                        border-bottom: 1px solid var(--sys-border);
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        background-color: #FAFAFA;
                    }}

                    .card-title {{
                        font-weight: 600;
                        font-size: 14px;
                    }}

                    /* WinUI 标准按钮 */
                    .win-btn {{
                        background-color: var(--sys-accent);
                        color: white;
                        border: 1px solid transparent;
                        border-radius: 4px;
                        padding: 5px 16px;
                        font-family: inherit;
                        font-size: 14px;
                        font-weight: 500;
                        cursor: pointer;
                        display: flex;
                        align-items: center;
                        gap: 8px;
                        box-shadow: inset 0 1px 0 rgba(255,255,255,0.1);
                        transition: all 0.1s ease-in-out;
                    }}

                    .win-btn:hover {{
                        background-color: var(--sys-accent-hover);
                    }}

                    .win-btn:active {{
                        background-color: var(--sys-accent-active);
                        color: rgba(255,255,255,0.7);
                    }}

                    .win-btn.success {{
                        background-color: #107C10;
                    }}

                    /* 代码展示区 */
                    pre {{
                        margin: 0;
                        padding: 20px;
                        font-family: 'Cascadia Code', 'Consolas', monospace;
                        font-size: 13px;
                        color: var(--sys-base-high);
                        white-space: pre-wrap;
                        word-wrap: break-word;
                        max-height: 450px;
                        overflow-y: auto;
                        background-color: var(--sys-card-bg);
                    }}

                    /* WinUI InfoBar 控件 */
                    .info-bar {{
                        background-color: var(--sys-success-bg);
                        color: var(--sys-success-text);
                        border: 1px solid rgba(15, 82, 7, 0.2);
                        border-radius: 4px;
                        padding: 12px 16px;
                        display: flex;
                        align-items: center;
                        gap: 12px;
                        font-size: 14px;
                    }}

                    .info-bar .icon {{
                        font-size: 16px;
                    }}
                </style>
            </head>
            <body>
                <div class="container">
                    <div class="header">
                        <h1 class="title"><span class="icon">&#xE8FB;</span> 认证完成</h1>
                        <p class="subtitle">Sauth JSON 数据已成功生成并就绪。</p>
                    </div>

                    <div class="card">
                        <div class="card-header">
                            <span class="card-title">有效载荷 (Payload)</span>
                            <button class="win-btn" id="copyBtn" onclick="copyJson()">
                                <span class="icon" id="copyIcon">&#xE8C8;</span>
                                <span id="copyText">复制 JSON</span>
                            </button>
                        </div>
                        <pre id="jsonContent">{escaped_json}</pre>
                    </div>

                    <div class="info-bar">
                        <span class="icon">&#xE946;</span>
                        <span>数据已自动写入系统剪贴板。您可以直接前往目标程序粘贴。</span>
                    </div>
                </div>

                <script>
                    function copyJson() {{
                        const jsonText = document.getElementById('jsonContent').textContent;
                        const btn = document.getElementById('copyBtn');
                        const icon = document.getElementById('copyIcon');
                        const text = document.getElementById('copyText');

                        const setSuccessState = () => {{
                            btn.classList.add('success');
                            icon.innerHTML = '&#xE8FB;'; // CheckMark icon
                            text.textContent = '已复制';

                            setTimeout(() => {{
                                btn.classList.remove('success');
                                icon.innerHTML = '&#xE8C8;'; // Copy icon
                                text.textContent = '复制 JSON';
                            }}, 2500);
                        }};

                        if (navigator.clipboard && window.isSecureContext) {{
                            navigator.clipboard.writeText(jsonText).then(setSuccessState).catch(fallbackCopy);
                        }} else {{
                            fallbackCopy(jsonText, setSuccessState);
                        }}
                    }}

                    function fallbackCopy(text, callback) {{
                        const textArea = document.createElement('textarea');
                        textArea.value = text;
                        textArea.style.position = 'fixed';
                        textArea.style.left = '-999999px';
                        document.body.appendChild(textArea);
                        textArea.focus();
                        textArea.select();
                        try {{
                            document.execCommand('copy');
                            if(callback) callback();
                        }} catch (err) {{
                            console.error('Fallback: Oops, unable to copy', err);
                        }}
                        document.body.removeChild(textArea);
                    }}

                    // 页面加载后自动复制
                    window.addEventListener('load', function() {{
                        setTimeout(copyJson, 300);
                    }});
                </script>
            </body>
            </html>
            """

            self.browser.SetPage(html_content, "")
            # print(f"[INJECTED] 页面已美化并注入 (WinUI 3 风格)")

        except Exception as e:
            print(f"注入失败: {e}")


if __name__ == "__main__":
    app = wx.App()
    frame = BrowserFrame()
    app.MainLoop()