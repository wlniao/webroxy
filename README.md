# Webroxy
这是一款基于TCP协议的Web反向代理网关，无数据库，通过DNS协议读取TXT解析记录获取后端服务器IP及端口号。
* 2019-12-21	新增短网址功能
* 2020-03-05	新增域名跳转功能
### Usage:
```
docker run -d --network host --restart=always \
-e ProxyHost=wln.io \
--name webroxy wlniao/webroxy
```