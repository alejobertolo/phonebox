package net.oxog.phonebox.deskpanel

import android.content.Context
import java.io.ByteArrayInputStream
import java.io.InputStream
import java.net.HttpURLConnection
import java.net.URL

/**
 * PhoneBox Lite: the phone can only read the Mac's four allowlisted endpoints
 * over adb reverse on USB. No pairing keys, Wi-Fi fallback or arbitrary URLs.
 */
class AgentLink(@Suppress("UNUSED_PARAMETER") context: Context) {
    class Reply(val code: Int, val contentType: String?, val body: InputStream)
    enum class Route { USB, NONE }

    @Volatile var route: Route = Route.NONE
        private set

    // Lite polls /api/state every second; the original SSE worker stays disabled.
    fun stream(@Suppress("UNUSED_PARAMETER") path: String): Pair<Route, Reply>? = null

    fun request(method: String, pathAndQuery: String): Reply {
        val path = pathAndQuery.substringBefore('?')
        if (method != "GET" || path !in API_READS) return error(403, "disabled in PhoneBox Lite")

        return try {
            val connection = URL("$USB_BASE$pathAndQuery").openConnection() as HttpURLConnection
            connection.instanceFollowRedirects = false
            connection.requestMethod = "GET"
            connection.connectTimeout = 500
            connection.readTimeout = 3500
            connection.useCaches = false
            connection.setRequestProperty("X-PhoneBox-App", "lite")

            val status = connection.responseCode
            if (status in 300..399) {
                connection.disconnect()
                return error(403, "redirects disabled")
            }
            val body = (if (status >= 400) connection.errorStream else connection.inputStream)
                ?: ByteArrayInputStream(ByteArray(0))
            route = Route.USB
            Reply(status, connection.contentType, body)
        } catch (_: Exception) {
            route = Route.NONE
            error(503, "USB disconnected")
        }
    }

    private fun error(status: Int, message: String): Reply =
        Reply(status, "application/json", ByteArrayInputStream(
            "{\"error\":\"$message\"}".toByteArray(Charsets.UTF_8)))

    companion object {
        private const val USB_BASE = "http://127.0.0.1:8765"
        private val API_READS = setOf("/api/state", "/api/config", "/api/catalog", "/api/info")
    }
}
