# redis�ͻ������

���ĸ�dll����github��Դ���� https://github.com/ServiceStack/ServiceStack �Ļ������޸���ÿСʱ6000�η��ʵ����ƣ�����˵����ServiceStack.Redis 6.9.1 ���ƽ�汾

## �޸�λ��
1. ServiceStack\ServiceStack.Redis\src\ServiceStack.Redis\RedisNativeClient_Utils.cs 
   ע����401��402 ��
2. ServiceStack\ServiceStack.Text\src\ServiceStack.Text\LicenseUtils.cs
   �޸�166�У�����RedisRequestPerHour��ֵ����6000�ĳ���int.MaxValue

Ȼ���޸�Release��ʽ���������ɼ�����ServiceStack\ServiceStack.Redis\src\ServiceStack.Redis\bin\Release\net6.0Ŀ¼�������ĸ�dll

## ע��
�ƽ���Ȩ������ѧϰ��������������