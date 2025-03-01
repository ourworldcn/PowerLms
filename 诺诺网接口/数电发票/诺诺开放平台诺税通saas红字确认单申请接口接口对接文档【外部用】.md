## nuonuo.OpeMplatform.saveInvoiceRedConfirm(诺税通saas红字确认单申请接口)

## 版本V2.

## 用于全电发票红字确认单申请（非部分冲红不需要传明细，部分冲红明细序号必须和原蓝票顺序保持一致）

# 请求地址

### 环境 请求地址

```
正式环境 https://sdk.nuonuo.com/open/v1/services
```
```
沙箱环境 https://sandbox.nuonuocs.cn/open/v1/services
```
## 注：请 下载SDK 并完成报文组装后发送接口调用请求，accessToken获取方式请参考 自用型应用创建 和 第三方应用创建 。

# 公共请求参数

## 消息头

### 名称 类型 是否必须 描述

```
Content-type String Y MIME类型【消息头】
```
```
X-Nuonuo-Sign String Y 签名（编码格式请使用UTF-8）【消息头】
```
```
accessToken String Y 授权码【消息头】
```
```
userTax String N 授权商户的税号（自用型应用非必填，第三方应用必填）【消息头】
```
```
method String Y 请求api对应的方法名称【消息头】
```
## 消息体

### 名称 类型 是否必须 描述

```
senid String Y 唯一标识，由企业自己生成32位随机码【消息体】
```
```
nonce String Y 8位随机正整数【消息体】
```
```
timestamp String Y 时间戳(当前时间的秒数)【消息体】
```
```
appkey String Y 平台分配给应用的appKey【消息体】
```
# 私有请求参数

### 名称 类型 是否必须 示例值 最大长度 描述

```
billId String N - 32
```
### 红字确认单申请号，需要保持唯一，不传

### 的话系统自动生成一个

```
blueInvoiceLine String Y pc 2
```
```
对应蓝票发票种类: bs:电子发票(增值税专
用发票)， pc:电子发票(普通发票)，es:全
电纸质发票(增值税专用发票)， ec:全电纸
质发票(普通发票)，p:普通发票（电票），
c:普通发票（纸票），s:专用发票，b:增值
税电子专用发票
```
```
applySource String Y 0 2 申请方（录入方）身份： 0 销方 1 购方
```
```
blueInvoiceNumber String N 20230508287375 20 对应蓝字发票号码（蓝票是增值税发票时
```

### 名称 类型 是否必须 示例值 最大长度 描述

### 668802 必传，长度为8位数字，若传20位数字则

### 视为是蓝字数电票号码）

blueInvoiceCode String N 12

### 对应蓝字发票代码（蓝票是增值税发票时

### 必传）

blueElecInvoiceNumber String N 20

### 对应蓝字数电票号码（数电普票、数电专

### 票、数纸普票、数纸专票都需要传，蓝票

### 是增值税发票时不传）

billTime String N

### 填开时间（时间戳格式），默认为当前时

### 间

sellerTaxNo String Y

### 91440101MA5AP

### D8X8J

### 20 销方税号

sellerName String Y

### 广东航天信息爱信诺

### 科技有限公司

### 100 销方名称，申请说明为销方申请时可为空

departmentId String N - 32 部门门店id（诺诺网系统中的id）

clerkId String N - 32 开票员id（诺诺网系统中的id）

buyerTaxNo String N

### 91330106MA2AY

### NWD8M

### 20 购方税号

buyerName String Y

### 浙江诺诺网络科技有

### 限公司

### 100 购方名称

vatUsage String N - 2

### 蓝字发票增值税用途（预留字段可为空）:

### 1 勾选抵扣 2 出口退税 3 代办出口退税 4

### 不抵扣

saleTaxUsage String N - 2 蓝字发票消费税用途（预留字段可为空）

accountStatus String N - 2

### 发票入账状态（预留字段可为空）： 0 未

### 入账 1 已入账

redReason String Y 2 2

### 冲红原因： 1销货退回 2开票有误 3服务中

### 止 4销售折让

extensionNumber String N 9998 5 分机号

autoInvoice String N 0 2

### 是否自动开票，0否（不传默认0）1是；传

### 1时，所申请的确认单变为购销双方已确认

### 或无需确认状态时，而自动开具红票。目

### 前该字段不生效，电票都自动开，纸票都

### 不自动

orderNo String N 20

### 若有值，则在无需确认或购销双方已确认

### 后去自动开红票，发票的开票单号用该值

phone String N 20

### 交付手机，红票优先取该值，不传默认取

### 原蓝票

email String N 50

### 交付邮箱，红票优先取该值，不传默认取

### 原蓝票

callbackUrl String N 225

### 确认单回传地址，回调内容可联系服务人

### 员；自动开票时，会透传到开票接口

- detail Array N

### 红字确认单明细信息列表（数电发票部分

### 冲红时才需要传）


### 名称 类型 是否必须 示例值 最大长度 描述

```
blueDetailIndex String Y 1 5
```
### 对应蓝票明细行序号，蓝票明细是折扣行

### 时只需传被折扣行的明细序号，折扣行的

### 序号不传（跳过）

```
goodsName String Y 服务 90 商品名称
```
```
unit String N 22 单位
```
```
specType String N 40 规格型号
```
```
num String N -1 16 数量
```
```
taxExcludedPrice String N 5 16 单价(不含税)为正数
```
```
taxExcludedAmount String N -5 16
```
### 商品金额(不含税)；带负号，精确到小数点

### 后面两位

```
taxAmount String N -0.3 16
```
### 商品税额，带负号，精确到小数点后面两

### 位

```
taxRate String Y 0.06 10 税率
```
```
goodsCode String Y
```
### 3049900000000

### 000000

### 19 商品编码

```
favouredPolicyFlag String N 2
```
### 优惠政策标识（01：简易征收 02：稀土产

### 品 03：免税 04：不征税 05：先征后退 0

### 6：100%先征后退 07：50%先征后退 0

### 8：按3%简易征收 09：按5%简易征收 1

### 0：按5%简易征收减按1.5%计征 11：即征

### 即退30% 12：即征即退50% 13：即征即

### 退70% 14：即征即退100% 15：超税负

### %即征即退 16：超税负8%即征即退 17

### ：超税负12%即征即退 18：超税负6%即

### 征即退）

# 响应参数

### 参数 类型 描述 示例值

```
code String 状态码 E
```
```
describe String 详细信息 成功
```
```
result String 红字确认单申请号 13123123123123123
```
# 请求示例

## JAVA

## NNOpenSDK sdk = NNOpenSDK.getIntance();

## String taxnum = "23***789"; // 授权企业税号

## String appKey = "Hn***XL";

## String appSecret = "F65***65F";

## String method = "nuonuo.OpeMplatform.saveInvoiceRedConfirm"; // API方法名

## String token = "2d484e**************pdui"; // 访问令牌

## String content = "{

## \"blueElecInvoiceNumber\": \"\",


## \"orderNo\": \"\",

## \"buyerTaxNo\": \"91330106MA2AYNWD8M\",

## \"vatUsage\": \"-\",

## \"saleTaxUsage\": \"-\",

## \"departmentId\": \"-\",

## \"blueInvoiceCode\": \"\",

## \"sellerName\": \"广东航天信息爱信诺科技有限公司\",

## \"applySource\": \"0\",

## \"clerkId\": \"-\",

## \"blueInvoiceLine\": \"pc\",

## \"buyerName\": \"浙江诺诺网络科技有限公司\",

## \"accountStatus\": \"-\",

## \"billTime\": \"\",

## \"sellerTaxNo\": \"91440101MA5APD8X8J\",

## \"autoInvoice\": \"0\",

## \"phone\": \"\",

## \"extensionNumber\": \"9998\",

## \"billId\": \"-\",

## \"blueInvoiceNumber\": \"20230508287375668802\",

## \"callbackUrl\": \"\",

## \"detail\": [

## {

## \"blueDetailIndex\": \"1\",

## \"specType\": \"\",

## \"taxRate\": \"0.06\",

## \"unit\": \"\",

## \"taxExcludedAmount\": \"-5\",

## \"num\": \"-1\",

## \"goodsCode\": \"3049900000000000000\",

## \"favouredPolicyFlag\": \"\",

## \"taxExcludedPrice\": \"5\",

## \"taxAmount\": \"-0.3\",

## \"goodsName\": \"服务\"

## }

## ],

## \"redReason\": \"2\",

## \"email\": \"\"

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

### E0000 成功


