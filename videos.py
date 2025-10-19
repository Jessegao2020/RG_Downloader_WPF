import scrapy
import json
import time
import requests

class VideosSpider(scrapy.Spider):
    name = "videos"
    custom_settings = {
        "LOG_ENABLED": False,
    }

    def __init__(self, user=None, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.user = user or None
        self.page = 1
        self.count = 40
        self.token = None
        self.token_expiry = 0
        self.headers = {
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36",
            "Referer": "https://www.redgifs.com/",
            "Origin": "https://www.redgifs.com",
            "Accept": "application/json, text/plain, */*"
        }

    def get_token(self):
        # 如果 token 还没过期，直接用
        if self.token and time.time() < self.token_expiry - 30:
            return self.token

        # 自动获取 guest token
        resp = requests.post("https://api.redgifs.com/v2/auth/guest")
        if resp.status_code != 200:
            self.logger.error(f"获取 token 失败: {resp.status_code}")
            return None
        data = resp.json()
        self.token = data.get("token")
        # 设置过期时间 (通常 token 有效期 ~2小时，这里假设 1.5小时)
        self.token_expiry = time.time() + 90 * 60
        return self.token

    def start_requests(self):
        token = self.get_token()
        if token:
            self.headers["Authorization"] = f"Bearer {token}"

        url = f"https://api.redgifs.com/v2/users/{self.user}/search?order=new&count={self.count}&page={self.page}"
        yield scrapy.Request(url, headers=self.headers, callback=self.parse, errback=self.errback)

    def parse(self, response):
        if response.status in [401, 403]:
            # token 可能过期，刷新一次再请求
            self.logger.info("可能是Token 过期，重新获取Token...")
            token = self.get_token()
            if token:
                self.headers["Authorization"] = f"Bearer {token}"
                yield scrapy.Request(response.url, headers=self.headers, callback=self.parse, errback=self.errback)
            return

        data = json.loads(response.text)
        gifs = data.get("gifs", [])
        if not gifs:
            return
	
        for gif in gifs:
            url = None
            if isinstance(gif.get("urls"), dict):
                url = gif["urls"].get("hd") or gif["urls"].get("sd")
            item = {
                "id": gif.get("id"),                
                "url": url                
            }
            print(json.dumps(item, ensure_ascii=False))
            yield item

        # 翻页
        self.page += 1
        next_url = f"https://api.redgifs.com/v2/users/{self.user}/search?order=new&count={self.count}&page={self.page}"
        yield scrapy.Request(next_url, headers=self.headers, callback=self.parse, errback=self.errback)

    def errback(self, failure):
        self.logger.error(repr(failure))
