## nuonuo.OpeMplatform.queryInvoiceRedConfirm(诺税通saas红字确认单查询接口)

## 版本V2.

## 用于查询数电发票的红字确认单

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
```
```
billStatus String N - 2
```
### 红字确认单状态（不传则查全部状态）：

### 01 无需确认 02 销方录入待购方确认 03

### 购方录入待销方确认 04 购销双方已确认 0

### 5 作废（销方录入购方否认） 06 作废

### （购方录入销方否认） 07 作废（超72小

### 时未确认） 08 作废（发起方已撤销） 09

### 作废（确认后撤销） 15 申请中 16 申请失

### 败

```
billId String N - 32 红字确认单申请号
```

### 名称 类型 是否必须 示例值 最大长度 描述

```
billNo String N - 32 红字确认单编号
billUuid String N - 32 红字确认单uuid
billTimeStart String N 2022-05-19 10 填开起始时间，确认单申请号/编号/值时，可为空，允许最大查询范围为90天uuid有
```
```
billTimeEnd String N 2022-06-19 10 填开结束时间，确认单申请号/编号/值时，可为空，允许最大查询范围为90天uuid有
pageSize String N 20 10 每页数量（默认10，最大50）
pageNo String N 1 10 当前页码（默认1）
```
# 响应参数

### 参数 类型 描述 示例值

```
code String 状态 0000
describe String 详细信息 查询成功
total String 总共条数 10
```
- list Array 红字确认单列表
    billNo String 红字确认单编号 1403011906000993
    billUuid String 红字确认单uuid 1d7f08b6ddb64cb19b095b0360f074d
    billId String 红字确认单申请号 1119316085226516480

```
billStatus String
```
### 红字确认单状态： 01 无需确认 02 销方录入待

### 购方确认 03 购方录入待销方确认 04 购销双方

### 已确认 05 作废（销方录入购方否认） 06 作废

### （购方录入销方否认） 07 作废（超72小时未确

### 认） 08 作废（发起方已撤销） 09 作废（确认

### 后撤销） 15 申请中 16 申请失败

### 01

```
billMessage String 描述
invoiceSerialNum String 红票流水号，若红字确认单已开红票（或已自动开红票）才会返回该值
```
```
orderNo String 红票订单号（开票单号），若红字确认单已开红票（或已自动开红票）才会返回该值
```
```
requestStatus String
```
### 操作状态：（根据操作方返回对应状态，可能为

### 空） 01 撤销中 02撤销失败 03 确认中 04 确认

### 失败

### 操作状态：（根据操作方返回对应状态，可能为空） 01 撤

### 销中 02撤销失败 03 确认中 04 确认失败

```
openStatus String 已开具红字发票标记： 1：已开具 0：未开具 0
applySource String 录入方身份： 0 销方 1 购方 0
```
```
blueInvoiceLine String
```
```
蓝字发票票种： bs：电子发票(增值税专用发票)
， pc：电子发票(普通发票)，es:全电纸质发票(
增值税专用发票)， ec:全电纸质发票(普通发票)
```
```
bs
```
```
blueInvoiceNumber String 对应蓝票号码 对应蓝票号码
blueInvoiceTime String 蓝字发票开票日期
```

### 参数 类型 描述 示例值

```
billTime String 申请日期 2020-03-26 18:44:
confirmTime String 确认日期 2020-03-27 18:44:
sellerTaxNo String 销方税号 150301199811285326
sellerName String 销方名称 测试税号
buyerTaxNo String 购方税号 150301199811285326
buyerName String 购方名称 测试税号
taxExcludedAmount String 冲红合计金额(不含税) -0.
taxAmount String 冲红合计税额 -0.
redReason String 冲红原因： 1销货退回 2开票有误 3服务中止 4销售折让
pdfUrl String 申请表pdf地址（暂不支持）
```
- detail Array 红字确认单明细信息列表
blueDetailIndex String 对应蓝票明细行序号 1
goodsName String 商品名称 苹果
unit String 单位 袋
specType String 规格型号 规格型号
num String 数量 -
taxExcludedPrice String 单价(不含税) 0.
taxExcludedAmount String 商品金额(不含税) -0.
taxAmount String 商品税额 -0.
taxRate String 税率 0.
goodsCode String 商品编码 4020000000000000000

```
favouredPolicyFlag String
```
### 01：简易征收 02：稀土产品 03：免税 04：不

### 征税 05：先征后退 06：100%先征后退 07：

### 0%先征后退 08：按3%简易征收 09：按5%简

### 易征收 10：按5%简易征收减按1.5%计征 11

### ：即征即退30% 12：即征即退50% 13：即征

### 即退70% 14：即征即退100% 15：超税负3%

### 即征即退 16：超税负8%即征即退 17：超税负

### 2%即征即退 18：超税负6%即征即退

### 0

```
price String 单价 0
```
# 请求示例

## JAVA

## NNOpenSDK sdk = NNOpenSDK.getIntance();

## String taxnum = "23***789"; // 授权企业税号

## String appKey = "Hn***XL";

## String appSecret = "F65***65F";


## String method = "nuonuo.OpeMplatform.queryInvoiceRedConfirm"; // API方法名

## String token = "2d484e**************pdui"; // 访问令牌

## String content = "{

## \"billUuid\": \"-\",

## \"identity\": \"0\",

## \"pageNo\": \"1\",

## \"billId\": \"-\",

## \"billTimeEnd\": \"2022-06-19\",

## \"billStatus\": \"-\",

## \"pageSize\": \"20\",

## \"billTimeStart\": \"2022-05-19\",

## \"billNo\": \"-\"

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


