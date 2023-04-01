/**
 * @license MIT
 * 
 * Written by Luuk Walstra
 * Discord: Luuk#8524
 * 
 * You're free to use this library as long as you keep this statement in this file
 */
class ScrollWithMe{
  constructor(element, options){
    if(typeof element !== 'string' && typeof element !== 'object') return console.error(`Invalid element! Element must be a string or an object`);
    if(typeof options !== 'object') return console.error(`Invalid options! Options must be an object`);

    this.options = options || {};
    this.element = typeof element === 'string' ? document.querySelector(element) : element;

    if(!this.element) return console.error(`Element does not exists!`);

    const startAt = typeof this.options.startAt === 'number' ? this.options.startAt : 0;
    const endAt = typeof this.options.endAt === 'number' ? this.options.endAt : Infinity;

    this.positioned = false;
    this.endpos = false;
    this.startPos = typeof this.options.setTop === 'number' ? this.options.setTop : this.element.offsetTop;
    this.startPosition = this.element.offsetTop;

    window.onscroll = async () => {
      this.scroll = document.body.scrollTop;
      if(this.scroll === 0 && document.documentElement.scrollTop !== 0) this.scroll = document.documentElement.scrollTop;

      if(this.scroll > startAt && this.positioned === false){
        this.positioned = true;
        this.element.style.position = `fixed`;
        this.element.style.top = `${this.startPos}px`;
        if(typeof this.options.callback === 'object'){
          if(typeof this.options.callback.onscroll === 'function') this.options.callback.onscroll();
        }
      } else if(this.scroll < startAt && this.positioned === true){
        this.positioned = false;
        this.element.style.position = ``;
        this.element.style.top = `${this.startPosition}px`;
        if(typeof this.options.callback === 'object'){
          if(typeof this.options.callback.onend === 'function') this.options.callback.onend();
        }
      } else if(this.scroll > endAt && this.endpos === false){
        this.endpos = true;
        this.element.style.position = ``;
        this.element.style.top = `${endAt + this.startPos}px`;
        if(typeof this.options.callback === 'object'){
          if(typeof this.options.callback.onend === 'function') this.options.callback.onend();
        }
      } else if(this.scroll < endAt && this.endpos === true){
        this.endpos = false;
        this.element.style.position = `fixed`;
        this.element.style.top = `${this.startPos}px`;
        if(typeof this.options.callback === 'object'){
          if(typeof this.options.callback.onscroll === 'function') this.options.callback.onscroll();
        }
      }
    };
  }
}
