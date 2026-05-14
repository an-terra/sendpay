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
    },
    lookupPostalCode: async function (zipcode) {
        try {
            const url = `https://zipcloud.ibsnet.co.jp/api/search?zipcode=${zipcode}`;
            const res = await fetch(url);
            if (!res.ok) return JSON.stringify({ error: 'HTTP ' + res.status });
            const json = await res.json();
            if (json.results && json.results.length > 0) {
                const r = json.results[0];
                return JSON.stringify({ prefecture: r.address1, city: r.address2 + r.address3 });
            }
            return JSON.stringify({ error: 'notfound' });
        } catch (e) {
            return JSON.stringify({ error: e.message });
        }
    }
};
