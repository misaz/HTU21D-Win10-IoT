Imports System.Runtime.InteropServices
Imports Windows.Devices.I2c

''' <summary>
''' Represent controlle for communication with HTU21D sensor. For Raspbery PI Connect SDA pin to GPIO02 (pin 3), SCL to GPIO03 (pin 5), VCC to 3.3V (pin 1 or 17) and GND to GND (pin 6 or 9 or 20 or 25 or 30 or 34 or 39). If communication fails it throws HtU21DCommunicationFailedException exception. Troubleshoting could be completed by IsPresent() method.
''' </summary>
Public Class HTU21DController

	Private Const devAddress = &H40
	Private settings = New I2cConnectionSettings(devAddress)
	Private i2cdev As I2cDevice

	Private Const CommunicationErrorMessageWithInner As String = "Communication with HTU21D sensor has failed. Use IsPresent() to verify if device is connected. Check if device is connected right way (see HTU21DController class comment for pinout). See inner exception for more details."
	Private Const CommunicationErrorMessage As String = "Communication with HTU21D sensor has failed. Use IsPresent() to verify if device is connected. Check if device is connected right way (see HTU21DController class comment for pinout)."

	Public Async Function GetI2cDevice() As Task(Of I2cDevice)
		If i2cdev Is Nothing Then
			i2cdev = (Await I2cController.GetDefaultAsync()).GetDevice(settings)
		End If
		Return i2cdev
	End Function

	Public Sub New(i2cController As I2cController)
		i2cdev = i2cController.GetDevice(settings)
	End Sub

	Public Sub New()
	End Sub

	Public Async Function SetResolution(mode As HTU21DResolutionMode) As Task
		Try
			Await WriteUserRegister(((Await ReadUserRegister()) And &H7E) Or mode)
		Catch ex As HTU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HTU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function

	''' <summary>
	''' Write 6 bit value to user register. 6 bit means that allowed range for value is 0 - 63. 6 bit restricton is because 2 bits of register are used to store sensor resolution.
	''' </summary>
	''' <param name="val"></param>
	''' <returns></returns>
	Public Async Function WriteUserRegisterValue(val As Byte) As Task
		Try
			If val > &B111111 Then
				Throw New ArgumentOutOfRangeException("User value saved to user register canot be larger than 6 bits.")
			End If
			Await WriteUserRegister(((Await ReadUserRegister()) And &H81) Or val << 1)
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function

	''' <summary>
	''' Read 6 bit value from register. 6 bit restricton is because 2 bits of register are used to store sensor resolution.
	''' </summary>
	''' <returns></returns>
	Public Async Function ReadUserRegisterValue() As Task(Of Byte)
		Try
			Dim raw = Await ReadUserRegister()
			Return (raw And &H7E) >> 1
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function


	''' <summary>
	''' Reads value from user register. LSb and MSb are used to store resulotuin mode.
	''' </summary>
	''' <returns></returns>
	Public Async Function ReadUserRegister() As Task(Of Byte)
		Try
			Dim dev = Await GetI2cDevice()
			Dim buff(0) As Byte

			If dev.WriteReadPartial(New Byte() {&HE7}, buff).Status <> I2cTransferStatus.FullTransfer Then
				Throw New HtU21DCommunicationFailedException(CommunicationErrorMessage)
			End If
			Return buff(0)
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function

	''' <summary>
	''' Writes an value to user register. LSb and MSb are used to store resulotuin mode.
	''' </summary>
	''' <param name="value"></param>
	''' <returns></returns>
	Public Async Function WriteUserRegister(value As Byte) As Task
		Try
			Dim dev = Await GetI2cDevice()
			If dev.WritePartial(New Byte() {&HE6, value}).Status <> I2cTransferStatus.FullTransfer Then
				Throw New HtU21DCommunicationFailedException(CommunicationErrorMessage)
			End If
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function

	''' <summary>
	''' Send Measure temperature command to sensor, receive data and convert data to °C.
	''' </summary>
	''' <returns>Measured temperature reported by sensor in °C.</returns>
	Public Async Function ReadTemperature() As Task(Of Double)
		Try

			Dim buff(2) As Byte
			Dim dev = Await GetI2cDevice()
			If dev.WriteReadPartial(New Byte() {&HE3}, buff).Status = I2cTransferStatus.FullTransfer Then
				Dim rawVal = CUInt(buff(0)) << 8 Or CUInt(buff(1))
				Return (rawVal * (175.72 / 65536.0)) - 46.85
			Else
				Throw New HtU21DCommunicationFailedException()
			End If
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function

	''' <summary>
	''' Send measure humidity command to sensor, receive data and convert data to %.
	''' </summary>
	''' <returns>Measured humidity in %.</returns>
	Public Async Function ReadHumidity() As Task(Of Double)
		Try
			Dim buff(2) As Byte
			Dim dev = Await GetI2cDevice()
			If dev.WriteReadPartial(New Byte() {&HE5}, buff).Status = I2cTransferStatus.FullTransfer Then
				Dim rawVal = CUInt(buff(0)) << 8 Or CUInt(buff(1))
				Return (rawVal * (125.25 / 65536.0)) - 6
			Else
				Throw New HtU21DCommunicationFailedException(CommunicationErrorMessage)
			End If
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function

	''' <summary>
	''' Detects if device si avalaible on the bus.
	''' </summary>
	Public Async Function IsPresent() As Task(Of Boolean)
		Try
			Dim dev = Await GetI2cDevice()
			dev.Write(New Byte() {0})
			Return False
		Catch ex As COMException
			Return ex.HResult = -2147023779
		Catch ex As Exception
			Return False
		End Try
	End Function

	''' <summary>
	''' Call SOFT REST command on sensor.
	''' </summary>
	Public Async Function ResetSensor() As Task
		Try
			Dim dev = Await GetI2cDevice()
			If dev.WritePartial(New Byte() {&HFE}).Status <> I2cTransferStatus.FullTransfer Then
				Throw New HtU21DCommunicationFailedException(CommunicationErrorMessage)
			End If
		Catch ex As HtU21DCommunicationFailedException
			Throw ex
		Catch ex As Exception
			Throw New HtU21DCommunicationFailedException(CommunicationErrorMessageWithInner, ex)
		End Try
	End Function
End Class
