# -*- coding: utf-8 -*-
"""Apply all post-V0.6 fixes with proper encoding handling."""
import os

path = r'D:\software\Q版本流星蝴蝶剑\Code\index_local_v03_enhanced.html'

# Read with utf-8-sig to handle potential BOM
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

# Verify Chinese is intact
assert '流星蝴蝶剑' in content or '龙城演武' in content, "Chinese content corrupted!"
print("Chinese content OK")

# 1. Add fullscreen button to zone-btns
zone_btns = content.find('id="zone-btns"')
if zone_btns > 0:
    close_div = content.find('</div>', zone_btns)
    fs_btn = '\n  <div style="padding:8px 16px;background:rgba(255,255,255,0.15);border:2px solid rgba(255,255,255,0.3);border-radius:8px;color:#ccc;font-size:13px;cursor:pointer;font-family:inherit" onclick="toggleFullscreen()">&#9974; 全屏</div>'
    content = content[:close_div] + fs_btn + content[close_div:]
    print("Added fullscreen button")

# 2. Add all new code before </script>
script_end = content.rfind('</script>')
new_code = r'''

/* === 全屏 === */
function toggleFullscreen() {
  if (!document.fullscreenElement) {
    document.documentElement.requestFullscreen().catch(function(){});
  } else {
    document.exitFullscreen();
  }
}

/* === 移动端触摸支持 === */
var touchState = { startX: 0, startY: 0, startTime: 0, active: false, isJoystick: false };

document.addEventListener('touchstart', function(e) {
  var touch = e.touches[0];
  touchState.startX = touch.clientX;
  touchState.startY = touch.clientY;
  touchState.startTime = Date.now();
  touchState.active = true;
  var w = window.innerWidth;
  if (touch.clientX < w * 0.3 && touch.clientY > window.innerHeight * 0.5) {
    touchState.isJoystick = true;
    touchState.joyCenterX = touch.clientX;
    touchState.joyCenterY = touch.clientY;
  }
}, { passive: false });

document.addEventListener('touchmove', function(e) {
  if (!touchState.active) return;
  var touch = e.touches[0];
  if (touchState.isJoystick && typeof state !== 'undefined' && state.player && !state.player.isDead) {
    var dx = touch.clientX - touchState.joyCenterX;
    var dy = touch.clientY - touchState.joyCenterY;
    var dist = Math.sqrt(dx*dx + dy*dy);
    var maxDist = 50;
    if (dist > 5) {
      var nx = dx / dist;
      var ny = dy / dist;
      var speed = Math.min(dist / maxDist, 1) * (typeof CONFIG !== 'undefined' ? CONFIG.MOVE_SPEED : 5) * (state.player.isDodging ? 2 : 1);
      state.player.position.x += nx * speed / 60;
      state.player.position.z += ny * speed / 60;
      state.player.rotation = Math.atan2(nx, ny);
    }
  }
  e.preventDefault();
}, { passive: false });

document.addEventListener('touchend', function(e) {
  var dt = Date.now() - touchState.startTime;
  var touch = e.changedTouches[0];
  var dx = touch.clientX - touchState.startX;
  var dy = touch.clientY - touchState.startY;
  var dist = Math.sqrt(dx*dx + dy*dy);
  if (dt < 200 && dist < 20 && !touchState.isJoystick) {
    if (typeof state !== 'undefined' && state.player && !state.player.isDead) {
      state.player.isAttacking = true;
      state.player.attackTimer = 0;
      if (typeof performAttack === 'function') performAttack(state.player, 'light');
    }
  }
  if (dt < 300 && dist > 60 && !touchState.isJoystick) {
    if (typeof state !== 'undefined' && state.player && !state.player.isDead && typeof CONFIG !== 'undefined' && state.player.stamina >= CONFIG.DODGE_STAMINA_COST) {
      state.player.isDodging = true;
      state.player.dodgeTimer = 0;
      state.player.dodgeDir = Math.atan2(dx, -dy);
      state.player.stamina -= CONFIG.DODGE_STAMINA_COST;
    }
  }
  touchState.active = false;
  touchState.isJoystick = false;
});

/* === 移动端虚拟按钮 === */
(function() {
  if ('ontouchstart' in window) {
    var c = document.createElement('div');
    c.style.cssText = 'position:fixed;bottom:20px;right:20px;z-index:17;pointer-events:auto;display:flex;gap:10px;';
    c.innerHTML =
      '<div ontouchstart="if(typeof state!==\'undefined\'&&state.player&&!state.player.isDead){state.player.isAttacking=true;state.player.attackTimer=0;if(typeof performAttack===\'function\')performAttack(state.player,\'light\')}" style="width:60px;height:60px;background:rgba(255,100,100,0.3);border:2px solid #ff4444;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:24px;color:#ff8888;user-select:none">\u2694</div>' +
      '<div ontouchstart="if(typeof state!==\'undefined\'&&state.player&&!state.player.isDead){state.player.isAttacking=true;state.player.attackTimer=0;if(typeof performAttack===\'function\')performAttack(state.player,\'heavy\')}" style="width:60px;height:60px;background:rgba(255,50,50,0.3);border:2px solid #ff2222;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:24px;color:#ff6666;user-select:none">\U0001f4a5</div>' +
      '<div ontouchstart="if(typeof state!==\'undefined\'&&state.player&&!state.player.isDead&&typeof CONFIG!==\'undefined\'&&state.player.stamina>=CONFIG.DODGE_STAMINA_COST){state.player.isDodging=true;state.player.dodgeTimer=0;state.player.dodgeDir=state.player.rotation;state.player.stamina-=CONFIG.DODGE_STAMINA_COST}" style="width:50px;height:50px;background:rgba(100,100,255,0.3);border:2px solid #4488ff;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:18px;color:#88bbff;user-select:none">\U0001f4a8</div>' +
      '<div ontouchstart="if(typeof state!==\'undefined\'&&state.player&&!state.player.isDead){state.player.isBlocking=!state.player.isBlocking}" style="width:50px;height:50px;background:rgba(100,200,100,0.3);border:2px solid #44cc44;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:18px;color:#88ff88;user-select:none">\U0001f6e1</div>';
    document.body.appendChild(c);
  }
})();

/* === 响应式CSS === */
var respCSS = document.createElement('style');
respCSS.textContent = '@media (max-width:768px){.char-grid{grid-template-columns:repeat(3,1fr)!important;gap:8px!important}.char-card{padding:8px!important;font-size:12px!important}.char-icon{font-size:28px!important}.map-grid{flex-direction:column!important;align-items:center!important}.map-card{width:90%!important;max-width:300px!important}#hud{font-size:12px!important}#minimap{width:120px!important;height:120px!important}#zone-btns{flex-wrap:wrap!important;justify-content:center!important}#zone-btns>div{padding:6px 10px!important;font-size:11px!important}}@media (max-width:480px){.char-grid{grid-template-columns:repeat(2,1fr)!important}#minimap{display:none!important}#zone-btns{top:auto!important;bottom:5px!important}}';
document.head.appendChild(respCSS);

/* === 快捷键帮助 === */
function showControlsHelp() {
  var old = document.getElementById('controls-help');
  if (old) { old.remove(); return; }
  var p = document.createElement('div');
  p.id = 'controls-help';
  p.style.cssText = 'position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);z-index:300;background:rgba(0,0,0,0.92);border:2px solid #ffd700;border-radius:12px;padding:24px;pointer-events:auto;min-width:320px;font-family:inherit;';
  p.innerHTML = '<div style="color:#ffd700;font-size:18px;font-weight:bold;margin-bottom:12px;text-align:center">\u64cd\u4f5c\u8bf4\u660e</div><div style="display:grid;grid-template-columns:auto 1fr;gap:6px 12px;font-size:13px"><span style="color:#ffd700">WASD</span><span style="color:#aaa">\u79fb\u52a8</span><span style="color:#ffd700">J</span><span style="color:#aaa">\u8f7b\u51fb</span><span style="color:#ffd700">K</span><span style="color:#aaa">\u91cd\u51fb</span><span style="color:#ffd700">\u7a7a\u683c</span><span style="color:#aaa">\u95ea\u907f</span><span style="color:#ffd700">Shift</span><span style="color:#aaa">\u683c\u6321</span><span style="color:#ffd700">E</span><span style="color:#aaa">\u6362\u6b66\u5668</span><span style="color:#ffd700">F11</span><span style="color:#aaa">\u5168\u5c4f</span><span style="color:#ffd700">H</span><span style="color:#aaa">\u5e2e\u52a9</span></div><button onclick="this.parentElement.remove()" style="margin-top:16px;padding:8px 24px;width:100%;background:#333;color:#fff;border:1px solid #ffd700;border-radius:6px;cursor:pointer;font-family:inherit">\u5173\u95ed</button>';
  document.body.appendChild(p);
}

document.addEventListener('keydown', function(e) {
  if (e.key === 'F11') { e.preventDefault(); toggleFullscreen(); }
  if ((e.key === 'h' || e.key === 'H') && document.activeElement.tagName !== 'INPUT') showControlsHelp();
});

setTimeout(function() {
  var btn = document.createElement('div');
  btn.style.cssText = 'position:fixed;bottom:30px;right:140px;z-index:16;pointer-events:auto;padding:4px 10px;background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.15);border-radius:4px;color:#666;font-size:11px;cursor:pointer;font-family:inherit;';
  btn.textContent = 'H: \u5e2e\u52a9';
  btn.onclick = showControlsHelp;
  document.body.appendChild(btn);
}, 100);

'''

content = content[:script_end] + new_code + content[script_end:]
print("Added fullscreen, touch, responsive, help")

# 3. Add shop button to zone-btns (if not already present from V0.6)
if 'showShopUI' not in content:
    zone_btns2 = content.find('id="zone-btns"')
    close_div2 = content.find('</div>', zone_btns2)
    shop_btn = '\n  <div style="padding:8px 16px;background:rgba(255,215,0,0.3);border:2px solid #ffd700;border-radius:8px;color:#ffd700;font-size:13px;cursor:pointer;font-family:inherit" onclick="showShopUI()">\U0001f3ea \u5546\u57ce</div>'
    content = content[:close_div2] + shop_btn + content[close_div2:]
    print("Added shop button")

# Write back as clean UTF-8 without BOM
with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

# Verify
with open(path, 'rb') as f:
    raw = f.read()
    has_bom = raw[:3] == b'\xef\xbb\xbf'
    print(f"Final file size: {len(raw)} bytes")
    print(f"Has BOM: {has_bom}")

with open(path, 'r', encoding='utf-8') as f:
    check = f.read()
    assert '流星蝴蝶剑' in check or '龙城演武' in check, "Chinese corrupted after edits!"
    print("Chinese intact after all edits!")

print("\nAll fixes applied successfully!")
