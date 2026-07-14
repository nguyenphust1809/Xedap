const chatBox = document.getElementById("chat-box");
const form = document.getElementById("chat-form");
const input = document.getElementById("user-input");
const clearBtn = document.getElementById("clear-btn");
const toggleBtn = document.getElementById("toggle-btn");
const chatModeLabel = document.getElementById("chat-mode");
const toggleAllSourcesBtn = document.getElementById("toggle-all-sources-btn");

let mode = "ai";
let allSourcesVisible = false;

// Lưu session
let sessionId = localStorage.getItem("chatSessionId") || crypto.randomUUID();
localStorage.setItem("chatSessionId", sessionId);

// Kết nối SignalR
const connection = new signalR.HubConnectionBuilder().withUrl("/chathub").build();
connection.on("ReceiveMessage", (sender, message) => {
    appendMessage(message.text, sender === "admin" ? "admin" : "bot", false, false, message.source || "");
});
connection.start();

// Gửi tin nhắn
form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const message = input.value.trim();
    if (!message) return;

    appendMessage(message, "user");
    input.value = "";

    const loadingId = appendMessage("...", "bot", true);

    if (mode === "ai") {
        try {
            const res = await fetch("/chat/assist", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ message, sessionId })
            });
            const data = await res.json();
            removeMessage(loadingId);
            appendMessage(data.reply || "❌ Không có phản hồi.", "bot", false, true, data.source || "");
        } catch (err) {
            removeMessage(loadingId);
            appendMessage("❌ Lỗi: " + err.message, "bot");
        }
    } else {
        await connection.invoke("SendMessage", sessionId, "user", message);
        removeMessage(loadingId);
    }
});

// Xóa hội thoại
clearBtn.addEventListener("click", () => {
    chatBox.innerHTML = `
        <div class="chat-row bot">
            <img src="https://cdn-icons-png.flaticon.com/512/4712/4712027.png" class="avatar" alt="Bot">
            <div class="chat-bubble bot-msg">
                👋 Xin chào! Tôi có thể giúp gì cho bạn hôm nay?
            </div>
        </div>`;
});

// Chuyển chế độ
toggleBtn.addEventListener("click", () => {
    if (mode === "ai") {
        mode = "human";
        chatModeLabel.textContent = "Đang chat với: 👨‍💼 Tư vấn viên";
        toggleBtn.textContent = "Chuyển sang chatbot 🤖";
    } else {
        mode = "ai";
        chatModeLabel.textContent = "Đang chat với: 🤖 Trợ lý AI";
        toggleBtn.textContent = "Chuyển sang tư vấn viên 👨‍💼";
    }
});

// Nút Hiện/Ẩn tất cả nguồn
toggleAllSourcesBtn.addEventListener("click", () => {
    const sources = document.querySelectorAll(".source-text");
    allSourcesVisible = !allSourcesVisible;

    sources.forEach(sourceEl => {
        sourceEl.style.display = allSourcesVisible ? "block" : "none";
    });

    const toggles = document.querySelectorAll(".toggle-source");
    toggles.forEach(toggle => toggle.textContent = allSourcesVisible ? "[Ẩn]" : "[Hiện]");

    toggleAllSourcesBtn.textContent = allSourcesVisible ? "Ẩn tất cả nguồn" : "Hiện tất cả nguồn";
});

// Hàm append tin nhắn
function appendMessage(text, sender, isLoading = false, typewriter = false, source = "") {
    const id = "msg-" + Math.random().toString(36).substring(2);
    const row = document.createElement("div");
    row.className = "chat-row " + sender;
    row.id = id;

    const avatar = document.createElement("img");
    avatar.className = "avatar";
    avatar.src = sender === "user"
        ? "https://cdn-icons-png.flaticon.com/512/1077/1077012.png"
        : sender === "admin"
            ? "https://cdn-icons-png.flaticon.com/512/219/219969.png"
            : "https://cdn-icons-png.flaticon.com/512/4712/4712027.png";

    const bubble = document.createElement("div");
    bubble.className = "chat-bubble " +
        (sender === "user" ? "user-msg" : sender === "admin" ? "admin-msg" : "bot-msg");

    if (isLoading) {
        bubble.innerHTML = `<div class="typing-dots"><span></span><span></span><span></span></div>`;
    } else if (typewriter) {
        typeWriterEffect(bubble, text);
    } else {
        bubble.textContent = text;
    }

    // Nếu có nguồn, thêm nút ẩn/hiện
    if (source && sender !== "user") {
        const container = document.createElement("div");
        container.style.marginTop = "5px";

        const sourceEl = document.createElement("div");
        sourceEl.className = "source-text";
        sourceEl.style.display = "none";
        sourceEl.textContent = "Nguồn: " + source;

        const toggleBtn = document.createElement("span");
        toggleBtn.className = "toggle-source";
        toggleBtn.textContent = "[Hiện]";
        toggleBtn.onclick = () => {
            if (sourceEl.style.display === "none") {
                sourceEl.style.display = "block";
                toggleBtn.textContent = "[Ẩn]";
            } else {
                sourceEl.style.display = "none";
                toggleBtn.textContent = "[Hiện]";
            }
        };

        container.appendChild(sourceEl);
        container.appendChild(toggleBtn);
        bubble.appendChild(container);
    }

    if (sender === "user") {
        row.appendChild(bubble);
        row.appendChild(avatar);
    } else {
        row.appendChild(avatar);
        row.appendChild(bubble);
    }

    chatBox.appendChild(row);
    chatBox.scrollTop = chatBox.scrollHeight;
    return id;
}

function removeMessage(id) {
    const msg = document.getElementById(id);
    if (msg) msg.remove();
}

function typeWriterEffect(element, text) {
    let i = 0;
    const speed = 15;
    function type() {
        if (i < text.length) {
            element.innerHTML += text.charAt(i);
            i++;
            chatBox.scrollTop = chatBox.scrollHeight;
            setTimeout(type, speed);
        }
    }
    type();
}
