## nuonuo.OpeMplatform.refreshInvoiceRedConfirm(诺税通saas红字确认单下载接口)

## 版本V2.

## 用于从税局下载全电发票红字确认单

# 请求地址

### 环境 请求地址

```
正式环境 https://sdk.nuonuo.com/open/v1/services
沙箱环境 https://sandbox.nuonuocs.cn/open/v1/services
```
## 注：请 下载SDK 并完成报文组装后发送接口调用请求，accessToken获取方式请参考 自用型应用创建 和 第三方应用创建 。

# 公共请求参数

## 消息头

### 名称 类型 是否必须 描述

```
Content-type String Y MIME类型【消息头】
X-Nuonuo-Sign String Y 签名（编码格式请使用UTF-8）【消息头】
accessToken String Y 授权码【消息头】
userTax String N 授权商户的税号（自用型应用非必填，第三方应用必填）【消息头】
method String Y 请求api对应的方法名称【消息头】
```
## 消息体

### 名称 类型 是否必须 描述

```
senid String Y 唯一标识，由企业自己生成32位随机码【消息体】
nonce String Y 8位随机正整数【消息体】
timestamp String Y 时间戳(当前时间的秒数)【消息体】
appkey String Y 平台分配给应用的appKey【消息体】
```
# 私有请求参数

### 名称 类型 是否必须 示例值 最大长度 描述

```
identity String Y 0 1 操作方身份： 0销方 1购方
billUuid String N - 32 红字确认单uuid
startTime String N 2022-05-19 10 填开起始时间
endTime String N 2022-06-19 10 填开结束时间
extensionNumber String N - 5 分机号
clerkId String N - 32 开票员id（诺诺网系统中的id）
deptId String N - 32 部门门店id（诺诺网系统中的id）
```

# 响应参数

### 参数 类型 描述 示例值

```
code String 状态码 E
describe String 详细信息 成功
```
- result Object 结果

# 请求示例

## JAVA

## NNOpenSDK sdk = NNOpenSDK.getIntance();

## String taxnum = "23***789"; // 授权企业税号

## String appKey = "Hn***XL";

## String appSecret = "F65***65F";

## String method = "nuonuo.OpeMplatform.refreshInvoiceRedConfirm"; // API方法名

## String token = "2d484e**************pdui"; // 访问令牌

## String content = "{

## \"billUuid\": \"-\",

## \"identity\": \"0\",

## \"extensionNumber\": \"-\",

## \"deptId\": \"-\",

## \"clerkId\": \"-\",

## \"startTime\": \"2022-05-19\",

## \"endTime\": \"2022-06-19\"

## }";

## String url = "https://sdk.nuonuo.com/open/v1/services"; // SDK请求地址

## String senid = UUID.randomUUID().toString().replace("-", ""); // 唯一标识，32位随机码，无需修改，保持默认即可

## String result = sdk.sendPostSyncRequest(url, senid, appKey, appSecret, token, taxnum, method, content);

## System.out.println(result);

# 响应示例

## JSON格式

# 返回码说明

## 公共异常码

### 返回码 返回描述 解决方案

### 9520 暂无

### 9540 暂无

### 9545 暂无


