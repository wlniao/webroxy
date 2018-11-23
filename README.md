# 未备案网站临时代理网关
这是一款基于TCP协议的Web反向代理工具，无数据库，通过DNS协议读取TXT解析记录获取后端服务器IP及端口号。
### 部署脚本
```
docker run -d -p80:80 -p6338:6338 --restart=always --name proxy wlniao/proxy:2.0.0
```