-- ����һ���Զ��庯����ģ��C#�е�FormatWith����
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'FN' AND name = 'FormatWithJson')
BEGIN
    DROP FUNCTION dbo.FormatWithJson;
END
GO

CREATE FUNCTION dbo.FormatWithJson(@template NVARCHAR(MAX), @jsonParams NVARCHAR(MAX))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @result NVARCHAR(MAX) = @template;
    
    -- �滻��������ռλ��
    SET @result = REPLACE(@result, '{LoginName}', ISNULL(JSON_VALUE(@jsonParams, '$.LoginName'), ''));
    SET @result = REPLACE(@result, '{CompanyName}', ISNULL(JSON_VALUE(@jsonParams, '$.CompanyName'), ''));
    SET @result = REPLACE(@result, '{DisplayName}', ISNULL(JSON_VALUE(@jsonParams, '$.DisplayName'), ''));
    SET @result = REPLACE(@result, '{OperationIp}', ISNULL(JSON_VALUE(@jsonParams, '$.OperationIp'), ''));
    SET @result = REPLACE(@result, '{OperationType}', ISNULL(JSON_VALUE(@jsonParams, '$.OperationType'), ''));
    SET @result = REPLACE(@result, '{ClientType}', ISNULL(JSON_VALUE(@jsonParams, '$.ClientType'), ''));
    SET @result = REPLACE(@result, '{CreateUtc}', CONVERT(VARCHAR(23), GETUTCDATE(), 126));
    
    -- ����������ܵĲ����滻...
    
    RETURN @result;
END
GO
