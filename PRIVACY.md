# Privacy Policy for VeSCU

Last updated: 2026-10-26

VeSCU only captures screen content (screenshots) when explicitly instructed by the user. Depending on what is currently displayed on your screen, this may include private or sensitive information. This data is stored to whatever path is set in the config, which is in your user's home directory by default. The screenshots only leave your device when you have explicitly set it, for example through OneDrive syncing or if the destination is an SMB network share. VeSCU itself never uploads any screenshots, nor can it explicitly instruct other apps to do so.

VeSCU does not retain any internal cache, history, or other copies of your screenshots, and never deletes any screenshots by itself. It also does not collect nor transmit any analytics or telemetry data.

VeSCU only ever connects to the internet if you explicitly permit it, through acceptance of starting an encoder download. It then downloads a manifest from `https://raw.githubusercontent.com/AsjerS/VeSCU/refs/heads/main/manifests/encoders-v1.json` to RAM, after which it may connect to any of the URLs listed in the manifest.

The developers of VeSCU are not responsible for the behaviour of external encoder programs. It is assumed those will also not make any data leave your device, though the exact behaviour is outside of the VeSCU developers' control.

For questions regarding this policy, [open an issue on the VeSCU GitHub repository](https://github.com/AsjerS/VeSCU/issues).
