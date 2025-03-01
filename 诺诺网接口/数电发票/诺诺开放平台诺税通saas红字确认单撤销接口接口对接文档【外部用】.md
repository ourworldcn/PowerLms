## nuonuo.OpeMplatform.confirmInfoCancel(诺税通saas红字确认单撤销接口)

## 版本V2.

## 调用该接口可撤销已发起的全电红字确认单 1、红字确认单的发起方为头部传入的税号，且红字信息单的状态为无需确认、购方录入待销方确认（撤销身份为 购方时

## ）、销方录入待购方确认（撤销身份为 销方时）、购销双方已确认（录入方为撤销方时） 2、有红字确认单申请号、红字确认单编号、红字确认单uuid都有时，优先

## 级： 红字确认单uuid > 红字确认单申请号 > 红字确认单编号，（这三个字段至少要传一个，不能全为空）

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
extensionNumber String N 5 分机号
clerkId String N 32 开票员id（诺诺网系统中的id）
deptId String N 32 部门id（诺诺网系统中的id）
billUuid String N 32 红字确认单uuid
billId String N 32 红字确认单申请号
billNo String N 44011222081000 32 红字确认单编号
```

### 名称 类型 是否必须 示例值 最大长度 描述

### 800025

```
identity String Y 0 1 操作方（撤销方）身份： 0 销方 1 购方
callbackUrl String N 225 回调地址，红字确认单撤销成功回传，回传内容可联系服务人员
```
# 响应参数

### 参数 类型 描述 示例值

```
code String 状态码 E
describe String 详细信息 请求成功
```
- result Object 结果

# 请求示例

## JAVA

## NNOpenSDK sdk = NNOpenSDK.getIntance();

## String taxnum = "23***789"; // 授权企业税号

## String appKey = "Hn***XL";

## String appSecret = "F65***65F";

## String method = "nuonuo.OpeMplatform.confirmInfoCancel"; // API方法名

## String token = "2d484e**************pdui"; // 访问令牌

## String content = "{

## \"billUuid\": \"\",

## \"extensionNumber\": \"\",

## \"identity\": \"0\",

## \"billId\": \"\",

## \"deptId\": \"\",

## \"clerkId\": \"\",

## \"callbackUrl\": \"\",

## \"billNo\": \"44011222081000800025\"

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

### E0000 请求成功


