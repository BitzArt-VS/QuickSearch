document.addEventListener('DOMContentLoaded', function () {
  function initTabs() {
    document.querySelectorAll('.tabs').forEach(function (tabsEl) {
      if (tabsEl.dataset.tabsInit) return;
      tabsEl.dataset.tabsInit = '1';

      var buttons = tabsEl.querySelectorAll('.tab-button');
      var contents = tabsEl.querySelectorAll('.tab-content');

      // Activate first tab
      if (buttons.length > 0) {
        buttons[0].classList.add('active');
        contents[0].classList.add('active');
      }

      buttons.forEach(function (btn) {
        btn.addEventListener('click', function () {
          var target = btn.getAttribute('data-tab');
          buttons.forEach(function (b) { b.classList.remove('active'); });
          contents.forEach(function (c) { c.classList.remove('active'); });
          btn.classList.add('active');
          var panel = tabsEl.querySelector('#tab-' + target);
          if (panel) panel.classList.add('active');
        });
      });
    });
  }

  initTabs();

  // Re-init on mdBook page navigation
  if (window.MutationObserver) {
    new MutationObserver(initTabs).observe(document.body, { childList: true, subtree: true });
  }
});
