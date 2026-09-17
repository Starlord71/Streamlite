# Diagrams

Detailed Mermaid diagrams for MediaConverter. GitHub renders Mermaid inside Markdown, so each file
below can be read directly.

| Diagram | What it shows |
| --- | --- |
| [Architecture](architecture.md) | Layered view of the WPF host, the Razor components, the App services, Core and the external tools. |
| [Core components](core-components.md) | Core interfaces, models, services and helper types with their relationships. |
| [Audio conversion](audio-conversion.md) | Sequence from picking a file to a finished conversion, including cancellation. |
| [Video download](video-download.md) | Sequence from entering a URL to a finished download, including the isolated work directory. |
| [Video to audio](video-to-audio.md) | Sequence for extracting MP3 audio from a local MP4 by reusing the audio converter. |
| [Binaries provisioning](binaries-provisioning.md) | First-run download and installation of ffmpeg and yt-dlp. |
| [UI operation states](ui-operation-states.md) | Tab lifecycle: empty, ready, running, success, cancelled and failed. |

The screenshots and the demo GIF live in [`../images`](../images).
