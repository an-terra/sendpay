window.spWidgets = {
    init: function (dotNetRef) {
        window.addEventListener('scroll', function () {
            dotNetRef.invokeMethodAsync('OnScrolled', window.scrollY > 300);
        });
    },
    scrollToTop: function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },
    scrollChatToBottom: function (el) {
        if (el) el.scrollTop = el.scrollHeight;
    }
};
