Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Data

Public Class Sturtevant
    Dim sock As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    Dim HiloComunicacionViva As Thread ' Nuevo hilo para mantener la comunicación viva
    Dim isConnected As Boolean = False
    Dim MID0061 As String = "" ' Variable para almacenar los bytes del rango 5 al 8
    Dim allReceivedMessages As New StringBuilder() ' Variable para almacenar todos los mensajes del servidor
    Dim isReceiving As Boolean = True ' Agrega esta línea para declarar isReceiving
    Dim keepAliveSemaphore As New AutoResetEvent(True)
    Private connectionTimer As Timer

    Public Property Angulo As String
    Public Property Torque As String
    Public Property Tiempo As String
    Public Property EstadoApriete As String

    Private _opcionSeleccionada As Integer = 1 ' Valor predeterminado
    Public Property opcionSeleccionada As Integer
        Get
            Return _opcionSeleccionada
        End Get
        Set(value As Integer)
            _opcionSeleccionada = value
        End Set
    End Property
    Private _serverIPAddress As String = "" ' Dirección IP predeterminada
    Public Property ServerIPAddress As String
        Get
            Return _serverIPAddress
        End Get
        Set(value As String)
            _serverIPAddress = value
        End Set
    End Property

    Private _serverPort As Integer = 8080 ' Puerto predeterminado
    Public Property ServerPort As Integer
        Get
            Return _serverPort
        End Get
        Set(value As Integer)
            _serverPort = value
        End Set
    End Property
    Public Function ConnectToServer(serverIPAddress As String, serverPort As Integer) As Boolean
        Try
            Dim serverAddress As IPAddress = IPAddress.Parse(serverIPAddress)
            Dim serverEndPoint As New IPEndPoint(serverAddress, serverPort)

            sock.Connect(serverEndPoint)
            isConnected = True

            ' Lista de mensajes a enviar
            Dim messagesToSend As New List(Of String) From {
                "00200001            " & Convert.ToChar(0),
                "00200060            " & Convert.ToChar(0),
                "00200040            " & Convert.ToChar(0),
                "00200018            " & Convert.ToChar(0),
                "00200053            " & Convert.ToChar(0),
                "00200042            " & Convert.ToChar(0)
            }

            ' Enviar mensajes uno por uno y esperar respuesta
            For Each message In messagesToSend
                sock.Send(Encoding.ASCII.GetBytes(message))
                WaitForResponse() ' Implement this method to wait for a response
            Next
            ' Iniciar el hilo de comunicación viva
            HiloComunicacionViva = New Thread(AddressOf KeepCommunicationAlive)
            HiloComunicacionViva.Start()

            Return True
        Catch ex As Exception
            ' Manejar excepciones según sea necesario
            Return False
        End Try
    End Function


    Private Sub WaitForResponse()
        ' Implementar lógica para esperar la respuesta del servidor
        ' Puedes utilizar un enfoque basado en tiempo o eventos, según tus necesidades
        ' Aquí, podrías esperar un tiempo fijo o usar eventos para manejar las respuestas.
        Thread.Sleep(500) ' Espera 2 segundos como ejemplo (ajusta según tus necesidades)
    End Sub

    Private Sub KeepCommunicationAlive()
        While True
            Try
                If sock.Connected Then
                    ' Enviar el mensaje de mantenimiento cada 5 segundos
                    Dim keepAliveMessage As String = "00209999            " & Convert.ToChar(0)
                    sock.Send(Encoding.ASCII.GetBytes(keepAliveMessage))

                    ' Actualizar el estado de la conexión
                    isConnected = True
                Else
                    ' Cerrar la conexión existente si está abierta
                    If sock.Connected Then
                        sock.Shutdown(SocketShutdown.Both)
                        sock.Close()
                    End If

                    ' Crear una nueva instancia de Socket
                    sock = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

                    ' Intentar la reconexión utilizando las variables de IP y puerto de la clase
                    Dim serverAddress As IPAddress = IPAddress.Parse(ServerIPAddress)
                    Dim serverEndPoint As New IPEndPoint(serverAddress, ServerPort)
                    sock.Connect(serverEndPoint)

                    ' Marcar como conectado si la conexión tiene éxito
                    isConnected = True

                    ' Enviar datos una vez establecida la conexión
                    Dim initialMessage As String = "00200001            " & Convert.ToChar(0)
                    sock.Send(Encoding.ASCII.GetBytes(initialMessage))
                End If
            Catch ex As Exception
                ' Manejar excepciones según sea necesario
                ' Puedes mostrar un mensaje de error o simplemente continuar
            End Try

            ' Esperar 5 segundos antes de enviar el próximo mensaje de mantenimiento
            Thread.Sleep(5000)
        End While
    End Sub


    Private Sub SendMessage(message As String)
        Try
            sock.Send(Encoding.ASCII.GetBytes(message))
        Catch ex As Exception
            ' Manejar excepciones, por ejemplo, si la conexión se cierra inesperadamente
            isConnected = False
        End Try
    End Sub
    Public Sub ConnectToServerAsync(serverIPAddress As String, serverPort As Integer)
        Dim connectThread As New Thread(Sub() ConnectToServer(serverIPAddress, serverPort))
        connectThread.Start()
    End Sub

    ' Método para iniciar la secuencia en un hilo separado
    Public Sub StartSequenceAsync()
        Dim sequenceThread As New Thread(AddressOf StartSequence)
        sequenceThread.Start()
    End Sub
    Public Sub StartSequence()
        ' Llama a la función Sequence y guarda los valores devueltos
        Dim sequenceResult = Sequence()

        ' Guarda los valores devueltos en las propiedades correspondientes
        Angulo = sequenceResult.Angulo
        Torque = sequenceResult.Torque
        Tiempo = sequenceResult.Tiempo
        EstadoApriete = sequenceResult.EstadoApriete
    End Sub

    Public Function Sequence() As (Angulo As String, Torque As String, Tiempo As String, EstadoApriete As String)
        Try

            ' Enviar el mensaje deseado al servidor
            Dim sequenceMessage As String = "00200043001         " & Convert.ToChar(0)
            sock.Send(Encoding.ASCII.GetBytes(sequenceMessage))

            ' Esperar un momento antes de enviar el próximo mensaje
            Thread.Sleep(1000)

            Dim mensaje As String = ""

            ' Determinar qué mensaje enviar según la opción seleccionada
            Select Case opcionSeleccionada
                Case 1
                    mensaje = "002300180010        001" & Convert.ToChar(0)
                Case 2
                    mensaje = "002300180010        002" & Convert.ToChar(0)
                Case 3
                    mensaje = "002300180010        003" & Convert.ToChar(0)
                Case 4
                    mensaje = "002300180010        004" & Convert.ToChar(0)
                Case 5
                    mensaje = "002300180010        005" & Convert.ToChar(0)
                Case 6
                    mensaje = "002300180010        006" & Convert.ToChar(0)
                    ' Agrega más casos según sea necesario
                Case Else
                    ' Mensaje predeterminado si la opción no es válida
                    mensaje = "002300180010        003" & Convert.ToChar(0)
            End Select

            ' Enviar el mensaje deseado al servidor
            sock.Send(Encoding.ASCII.GetBytes(mensaje))

            ' Esperar un momento después de enviar el segundo mensaje, si es necesario
            Thread.Sleep(1000)
            ' Leer el resultado del servidor
            Dim data(1023) As Byte
            sock.Receive(data)
            Dim resultText As String = Encoding.ASCII.GetString(data).TrimEnd(ControlChars.NullChar)

            ' Validar la condición específica antes de procesar los resultados
            If Not CondicionEspecifica(resultText) Then
                ' Si el dato no es "0061", seguir leyendo hasta recibir "0061"
                While Not CondicionEspecifica(resultText)
                    ' Leer el resultado del servidor nuevamente
                    sock.Receive(data)
                    resultText = Encoding.ASCII.GetString(data).TrimEnd(ControlChars.NullChar)
                End While
            End If

            ' Procesar los resultados solo si se cumple la condición
            If CondicionEspecifica(resultText) Then

                ' Procesar los bytes del rango 170 al 174 (Angulo)
                Dim byteArrayAngulo() As Byte = Encoding.ASCII.GetBytes(resultText)
                Dim selectedBytesAngulo(18) As Byte ' Tamaño del rango (174 - 170 + 1)
                Array.Copy(byteArrayAngulo, 169, selectedBytesAngulo, 0, 5)
                Dim Angulo As String = Encoding.ASCII.GetString(selectedBytesAngulo)

                ' Procesar los bytes del rango 177 al 195 (Tiempo)
                Dim byteArrayTiempo() As Byte = Encoding.ASCII.GetBytes(resultText)
                Dim selectedBytesTiempo(18) As Byte ' Tamaño del rango (216 - 198 + 1)
                Array.Copy(byteArrayTiempo, 176, selectedBytesTiempo, 0, 19)
                Dim Tiempo As String = Encoding.ASCII.GetString(selectedBytesTiempo)

                ' Procesar los bytes del rango 141 al 146 (Torque)
                Dim byteArrayTorque() As Byte = Encoding.ASCII.GetBytes(resultText)
                Dim selectedBytesTorque(18) As Byte ' Tamaño del rango (146 - 141 + 1)
                Array.Copy(byteArrayTorque, 140, selectedBytesTorque, 0, 6)
                Dim Torque As String = Encoding.ASCII.GetString(selectedBytesTorque)

                ' Torque = ConvertirTorque(Torque)

                ' Procesar los bytes del rango 108 (Estado de apriete)
                Dim byteArrayEstadoApriete() As Byte = Encoding.ASCII.GetBytes(resultText)
                Dim selectedBytesEstadoApriete(18) As Byte ' Tamaño del rango (108 + 1)
                Array.Copy(byteArrayEstadoApriete, 107, selectedBytesEstadoApriete, 0, 1)
                Dim EstadoApriete As String = Encoding.ASCII.GetString(selectedBytesEstadoApriete)

                ' Enviar mensaje 00200062 al servidor
                Dim MID00620 As String = "00200062            " & Convert.ToChar(0)
                sock.Send(Encoding.ASCII.GetBytes(MID00620))

                ' Devolver los valores procesados
                Return ((Angulo / 100).ToString("0.00"), (Torque / 100).ToString("0.00"), Tiempo, EstadoApriete)
            End If
            ' Envía el mensaje de confirmación 00200042 al servidor al final de cada ciclo
            Dim MID0042 As String = "00200042            " & Convert.ToChar(0)
            sock.Send(Encoding.ASCII.GetBytes(MID0042))

            ' En caso de que no se haya procesado correctamente, devolvemos valores por defecto
            Return ("", "", "", "")
        Catch ex As Exception
            ' Manejar excepciones según sea necesario
            ' Puedes mostrar un mensaje de error o simplemente salir del bucle
            ' DisconnectAndReset()
            Return ("", "", "", "")

        Finally
            ' Restablecer la interfaz de usuario después de completar la secuencia (si es necesario)
            Dim MID0042 As String = "00200042            " & Convert.ToChar(0)
            sock.Send(Encoding.ASCII.GetBytes(MID0042))
        End Try
    End Function

    Public Function GetSequenceData() As (Angulo As String, Torque As String, Tiempo As String, EstadoApriete As String)
        ' Devuelve una tupla con los valores actuales
        Return (Angulo, Torque, Tiempo, EstadoApriete)
    End Function

    Private Sub ResetServerMessages()
        ' Restablecer otros valores si es necesario
    End Sub

    Private Sub ReconnectToServer()
        Try
            ' Cerrar la conexión existente si está abierta
            If sock.Connected Then
                sock.Shutdown(SocketShutdown.Both)
                sock.Close()
            End If

            ' Crear una nueva instancia de Socket
            sock = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

            ' Intentar la reconexión
            Dim serverAddress As IPAddress = IPAddress.Parse(ServerIPAddress)
            Dim serverEndPoint As New IPEndPoint(serverAddress, ServerPort)
            sock.Connect(serverEndPoint)
            isConnected = True
            ' Enviar datos una vez al establecer la conexión
            Dim initialMessage As String = "00200001            " & Convert.ToChar(0)
            sock.Send(Encoding.ASCII.GetBytes(initialMessage))
        Catch ex As Exception
            ' Manejar excepciones según sea necesario
            ' Puedes mostrar un mensaje de error o simplemente continuar
        End Try
    End Sub

    Private Function CondicionEspecifica(resultText As String) As Boolean
        ' Verificar si el dato a validar es "0061" o "0060"
        Dim byteArrayMID() As Byte = Encoding.ASCII.GetBytes(resultText)
        Dim selectedBytesMID(3) As Byte ' Tamaño del rango (8 - 5 + 1)
        Array.Copy(byteArrayMID, 4, selectedBytesMID, 0, 4)
        Dim MID As String = Encoding.ASCII.GetString(selectedBytesMID)

        ' Si MID es igual a "0061" o "0060", se cumple la condición y se puede continuar
        Return MID = "0061" OrElse MID = "0060"
    End Function

    Public Sub DisconnectFromServer()
        Try
            ' Verificar si el socket está conectado
            If isConnected Then
                ' Cerrar la conexión existente
                sock.Shutdown(SocketShutdown.Both)
                sock.Close()
                isConnected = False
                ' Detener el hilo de comunicación viva si está en ejecución
                If HiloComunicacionViva IsNot Nothing AndAlso HiloComunicacionViva.IsAlive Then
                    HiloComunicacionViva.Abort()
                End If
            End If
        Catch ex As Exception
            ' Manejar excepciones según sea necesario
            ' Puedes mostrar un mensaje de error o simplemente continuar
        End Try
    End Sub

End Class
