## nuonuo.OpeMplatform.confirm(诺税通saas红字确认单确认接口)

## 版本V2.

## 用于购方/销方 进行红字确认单的确认 1、销方录入待购方确定 的时候，才能以购方的身份确认 2、购方录入待销方确定 的时候，才能以销方的身份确认 3、有红字确

## 认单申请号、红字确认单编号、红字确认单uuid都有时，优先级： 红字确认单uuid > 红字确认单申请号 > 红字确认单编号，（这三个字段至少要传一个，不能全为空

## ）

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
billNo String N 440112220710031 32 红字确认单编号
```

### 名称 类型 是否必须 示例值 最大长度 描述

### 00064

```
identity String Y 1 1 操作方（确认方）身份： 0：销方 1：购方
confirmAgreement String Y 1 1 处理意见： 0：拒绝 1：同意
confirmReason String N 完全同意 200 处理理由
```
```
callbackUrl String N 225
```
### 回调地址，红字确认单确认完毕回传，回

### 传内容可联系服务人员；确认后若自动开

### 具红票，可将该回调地址透传至开票接口

### ，红票自动开具成功后自动回调开票结果

```
orderNo String N 64
```
### 红票订单号（开票单号），红字确认单若

### 确认后自动开红票，则可传该值作为自动

### 开具红票的订单号（开票单号）

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

## String method = "nuonuo.OpeMplatform.confirm"; // API方法名

## String token = "2d484e**************pdui"; // 访问令牌

## String content = "{

## \"orderNo\": \"\",

## \"billUuid\": \"\",

## \"extensionNumber\": \"\",

## \"identity\": \"1\",

## \"billId\": \"\",

## \"confirmReason\": \"完全同意\",

## \"deptId\": \"\",

## \"confirmAgreement\": \"1\",

## \"clerkId\": \"\",

## \"callbackUrl\": \"\",

## \"billNo\": \"44011222071003100064\"

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


