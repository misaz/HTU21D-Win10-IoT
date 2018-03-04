''' <summary>
''' Emeration of avalaible measurement precision modes.
''' </summary>
Public Enum HTU21DResolutionMode
	''' <summary>
	''' Humidity is measured by 12 bit value adn temperature by 14 bit value. This is the most precise mode you can select.
	''' </summary>
	Humidity12_Temperature14 = &H0

	''' <summary>
	''' Humidity is measured by 8 bit value adn temperature by 12 bit value.
	''' </summary>
	Humidity8_Temperature12 = &H1

	''' <summary>
	''' Humidity is measured by 10 bit value adn temperature by 13 bit value.
	''' </summary>
	Humidity10_Temperature13 = &H80

	''' <summary>
	''' Humidity is measured by 11 bit value adn temperature by 11 bit value.
	''' </summary>
	Humidity11_Temperature11 = &H81
End Enum
