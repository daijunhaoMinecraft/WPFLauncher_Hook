"""
作者的话
说实在的这东西没啥好开源的
其实就是 codexus 里面的代码提取出来+AI处理的
需 Python 安装模块 requests
"""
import requests
import uuid
import json
import base64
import random
import string
import urllib3
import os
import time

# =================配置区域=================
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

# 常量定义
PROJECT_ID = "x19"
MPAY_HOST = "https://service.mkey.163.com"
GAME_VERSION = "c1.25.0" 
CACHE_FILE = "device_cache.json"

HEADERS = {
    "User-Agent": "WPFLauncher/0.0.0.0",
    "Content-Type": "application/x-www-form-urlencoded"
}

# =================辅助函数=================
def generate_mac():
    return ":".join(["{:02x}".format(random.randint(0, 255)) for _ in range(6)])

def generate_random_string(length=12):
    return "".join(random.choices(string.ascii_letters + string.digits, k=length))

def base64_encode(s):
    return base64.b64encode(s.encode('utf-8')).decode('utf-8')

# =================核心逻辑=================
class X19LoginBot:
    def __init__(self):
        self.session = requests.Session()
        self.session.verify = False
        self.session.headers.update(HEADERS)
        self.unique_id = None
        self.device_id = None

    def _get_base_params(self):
        return {
            "app_channel": "netease",
            "app_mode": "2",
            "app_type": "games",
            "arch": "win_x64",
            "cv": "c4.2.0",
            "mcount_app_key": "EEkEEXLymcNjM42yLY3Bn6AO15aGy4yq",
            "mcount_transaction_id": "0",
            "process_id": "1000",
            "sv": "10.0.22621",
            "updater_cv": "c1.0.0",
            "game_id": PROJECT_ID,
            "gv": GAME_VERSION
        }

    def initialize_device(self):
        """加载或注册设备"""
        if os.path.exists(CACHE_FILE):
            try:
                with open(CACHE_FILE, 'r', encoding='utf-8') as f:
                    cache_data = json.load(f)
                self.device_id = cache_data.get('device_id')
                if self.device_id:
                    print(f"[*] 从缓存加载设备成功 ID: {self.device_id}")
                    return
            except Exception:
                pass

        self._register_new_device()

    def _register_new_device(self):
        self.unique_id = uuid.uuid4().hex
        url = f"{MPAY_HOST}/mpay/games/{PROJECT_ID}/devices"
        
        params = self._get_base_params()
        params.update({
            "unique_id": self.unique_id,
            "brand": "Microsoft",
            "device_model": "pc_mode",
            "device_name": f"PC-{generate_random_string()}",
            "device_type": "Computer",
            "init_urs_device": "0",
            "mac": generate_mac(),
            "resolution": "1920x1080",
            "system_name": "windows",
            "system_version": "10.0.22621"
        })

        try:
            print(f"[*] 正在注册新设备...")
            resp = self.session.post(url, data=params)
            resp.raise_for_status()
            data = resp.json()
            self.device_id = data['device']['id']
            
            with open(CACHE_FILE, 'w', encoding='utf-8') as f:
                json.dump({"unique_id": self.unique_id, "device_id": self.device_id}, f)
            print(f"[+] 新设备注册成功: {self.device_id}")
        except Exception as e:
            print(f"[-] 设备注册失败: {e}")
            exit(1)

    def send_sms(self, phone):
        """
        发送验证码请求
        返回: (status_type, data)
        status_type: "SUCCESS", "UPSTREAM_REQUIRED", "FAIL"
        """
        url = f"{MPAY_HOST}/mpay/api/users/login/mobile/get_sms"
        params = self._get_base_params()
        params.update({"device_id": self.device_id, "mobile": phone})
        
        try:
            resp = self.session.post(url, data=params)
            # 尝试解析 JSON
            try:
                data = resp.json()
            except:
                data = {}

            if resp.status_code == 200:
                return "SUCCESS", None
            
            # 检查是否为上行短信验证 (Code 1373)
            if data.get('code') == 1373:
                return "UPSTREAM_REQUIRED", data.get('reply_sms', {})
            
            # 其他错误
            return "FAIL", data.get('reason', 'Unknown error')

        except Exception as e:
            return "FAIL", str(e)

    def verify_sms(self, phone, code="", up_content=""):
        """
        验证短信
        code: 用户输入的验证码 (下行模式)
        up_content: 上行短信的内容 (上行模式)
        """
        url = f"{MPAY_HOST}/mpay/api/users/login/mobile/verify_sms"
        params = self._get_base_params()
        params.update({
            "device_id": self.device_id, 
            "mobile": phone, 
            "smscode": code,         # 上行模式时为空字符串
            "up_content": up_content # 普通模式时为空字符串
        })
        
        resp = self.session.post(url, data=params)
        
        if resp.status_code == 200:
            return True, resp.json().get('ticket')
        else:
            try:
                msg = resp.json().get('reason', 'Verify failed')
            except:
                msg = f"HTTP {resp.status_code}"
            return False, msg

    def complete_login(self, phone, ticket):
        encoded_phone = base64_encode(phone)
        url = f"{MPAY_HOST}/mpay/api/users/login/mobile/finish?un={encoded_phone}"
        params = self._get_base_params()
        params.update({
            "device_id": self.device_id,
            "ticket": ticket,
            "opt_fields": "nickname,avatar,realname_status,mobile_bind_status,mask_related_mobile,related_login_status"
        })
        resp = self.session.post(url, data=params)
        return resp.json() if resp.status_code == 200 else None

# =================主程序=================
if __name__ == "__main__":
    bot = X19LoginBot()
    bot.initialize_device()
    
    phone_number = input("请输入手机号: ")
    
    # 1. 发起验证流程
    sms_status, sms_data = bot.send_sms(phone_number)
    
    ticket = None
    
    # 2. 根据服务器返回决定验证方式
    if sms_status == "SUCCESS":
        print(f"[*] 验证码已发送至 {phone_number} (普通模式)")
        
        # 普通验证码重试循环
        while True:
            sms_code = input("请输入验证码: ")
            success, result = bot.verify_sms(phone_number, code=sms_code, up_content="")
            
            if success:
                ticket = result
                print("[+] 验证成功！")
                break
            else:
                print(f"[-] 验证失败: {result}")
                retry = input("是否重试输入验证码? (y/n): ")
                if retry.lower() != 'y':
                    break

    elif sms_status == "UPSTREAM_REQUIRED":
        print("\n" + "!"*40)
        print("[!] 触发上行短信验证 (需您主动发送短信)")
        print(f"[!] 请使用手机 {phone_number} 发送短信:")
        print(f"[!] 内容: {sms_data.get('content')}")
        print(f"[!] 发送至: {sms_data.get('number')}")
        print("!"*40 + "\n")
        
        up_content = sms_data.get('content')
        
        # 上行短信重试循环 (轮询等待)
        while True:
            input(">>> 发送完成后，请按回车键继续验证...")
            
            print("[*] 正在检查服务器接收状态...")
            # 此时 smscode 为空，up_content 为必须参数
            success, result = bot.verify_sms(phone_number, code="", up_content=up_content)
            
            if success:
                ticket = result
                print("[+] 服务器确认接收成功！")
                break
            else:
                print(f"[-] 验证失败: {result} (可能服务器稍有延迟)")
                retry = input("是否再次检查? (y/n): ")
                if retry.lower() != 'y':
                    break
                    
    else:
        print(f"[-] 发起验证请求失败: {sms_data}")

    # 3. 只有获取到 Ticket 才继续
    if ticket:
        print(f"[*] 正在完成登录流程...")
        user_wrapper = bot.complete_login(phone_number, ticket)
        
        if user_wrapper and 'user' in user_wrapper:
            user = user_wrapper['user']
            print(f"[+] 登录成功! UserID: {user['id']}")
            
            sauth_dict = {
                "gameid": "x19",
                "login_channel": "netease",
                "app_channel": "netease",
                "platform": "pc",
                "sdkuid": user['id'],
                "sessionid": user['token'],
                "sdk_version": "4.2.0",
                "udid": uuid.uuid4().hex.upper(),
                "deviceid": bot.device_id,
                "aim_info": '{"aim":"127.0.0.1","country":"CN","tz":"+0800","tzid":""}'
            }
            
            final_output = {
                "sauth_json": json.dumps(sauth_dict, separators=(',', ':'))
            }
            
            print("\n" + "="*20 + " RESULT " + "="*20)
            print(json.dumps(final_output))
            print("="*48)
        else:
            print("[-] 最终登录失败，接口返回异常")
    else:
        print("[-] 流程结束，未获取到登录凭证。")

input("按回车键退出程序")