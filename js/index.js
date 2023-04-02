var scone, topIdle;
var mx, my;

window.addEventListener("load", function() {

    scone = document.getElementById('sc1');
    topIdle = document.getElementById('topIdleBack');
    
    document.addEventListener("mousemove", mouseUpdate);
        
    window.addEventListener( "scroll", (event) => {scrolly()}, false);
    
    /*new ScrollWithMe(document.querySelector(`#topIdleBack`), {
            startAt: 0.01,
            endAt: window.innerHeight,
            setTop: 0,
            });*/
        
    parallax(null);

})

function mouseUpdate(event) {
    
    mx = event.pageX;
    my = event.pageY;
    
    scrolly();
}

function scrolly(){
    var x = ((window.innerWidth/2) - mx) / 90;
    var y = ((window.innerHeight/2) - my) / 90;

    var docHeight = document.documentElement.offsetHeight;
    var scrolled = (window.scrollY / ( 700 ))+0.8;
    if(scrolled < 0.95) scrolled = 0.95;
    if(scrolled > 1.1) scrolled = 1.1;
    
    var transformValue = `translateX(${x}px) translateY(${y}px) scale(${scrolled})`;
    
    scone.style.WebkitTransform = transformValue;
    scone.style.MozTransform = transformValue;
    scone.style.OTransform = transformValue;
    scone.style.transform = transformValue;
    
    var scale;
    if(window.innerHeight<window.innerWidth*0.65){
        scale = window.innerHeight;
    }else{
        scale = window.innerWidth*0.65;
    }
    if(window.scrollY/scale>0.5){
        topIdle.style.visibility = "hidden";
    }else{
        topIdle.style.visibility = "visible";
        topIdle.style.opacity = 1.3-((window.scrollY/scale)*1.8);
        topIdle.style.transform = "scale(" + (1-((window.scrollY/scale)/5)) + ")";
    }
}