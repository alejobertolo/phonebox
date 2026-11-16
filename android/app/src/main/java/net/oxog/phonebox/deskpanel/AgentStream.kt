package net.oxog.phonebox.deskpanel

/**
 * PhoneBox Lite polls the local USB agent every second. It does not open
 * background streams, reconnect over Wi-Fi or accept remote notifications.
 */
class AgentStream(
    @Suppress("UNUSED_PARAMETER") link: AgentLink,
    @Suppress("UNUSED_PARAMETER") deliver: (event: String, data: String) -> Unit
) {
    fun start() { /* intentionally disabled */ }
    fun stop() { /* intentionally disabled */ }
}
